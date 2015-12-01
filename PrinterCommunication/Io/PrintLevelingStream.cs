﻿/*
Copyright (c) 2014, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using MatterHackers.Agg;
using MatterHackers.GCodeVisualizer;
using MatterHackers.VectorMath;
using System.Text;
using System.Collections.Generic;
using MatterHackers.MatterControl.ConfigurationPage.PrintLeveling;

namespace MatterHackers.MatterControl.PrinterCommunication.Io
{
    public class PrintLevelingStream : GCodeStream
    {
        GCodeStream internalStream;
        PrinterMove lastDestination = new PrinterMove();
        private event EventHandler unregisterEvents;

        public PrintLevelingStream(GCodeStream internalStream)
        {
            this.internalStream = internalStream;

            PrinterConnectionAndCommunication.Instance.PositionRead.RegisterEvent(PrinterReportedPosition, ref unregisterEvents);
        }

        private void PrinterReportedPosition(object sender, EventArgs e)
        {
            lastDestination = new PrinterMove(PrinterConnectionAndCommunication.Instance.CurrentDestination,
                PrinterConnectionAndCommunication.Instance.CurrentExtruderDestination,
                PrinterConnectionAndCommunication.Instance.CurrentFeedRate);
        }

        public override void Dispose()
        {
            unregisterEvents?.Invoke(this, null);
            internalStream.Dispose();
        }

        public override string ReadLine()
        {
            string lineFromChild = internalStream.ReadLine();

            if (lineFromChild != null
                && PrinterConnectionAndCommunication.Instance.ActivePrinter.DoPrintLeveling
                && LineIsMovement(lineFromChild))
            {
                PrinterMove currentDestination = GetPosition(lineFromChild, lastDestination);
                string leveledLine = RunPrintLevelingTranslations(lineFromChild, currentDestination);

                if(leveledLine != lineFromChild)
                {
                    int a = 0;
                }

                lastDestination = currentDestination;
                return leveledLine;
            }

            return lineFromChild;
        }

        private string RunPrintLevelingTranslations(string lineBeingSent, PrinterMove currentDestination)
        {
            PrintLevelingData levelingData = PrintLevelingData.GetForPrinter(ActivePrinterProfile.Instance.ActivePrinter);
            if (levelingData != null)
            {
                switch (levelingData.CurrentPrinterLevelingSystem)
                {
                    case PrintLevelingData.LevelingSystem.Probe2Points:
                        lineBeingSent = LevelWizard2Point.ApplyLeveling(lineBeingSent, currentDestination.position, PrinterMachineInstruction.MovementTypes.Absolute);
                        break;

                    case PrintLevelingData.LevelingSystem.Probe3Points:
                        lineBeingSent = LevelWizard3Point.ApplyLeveling(lineBeingSent, currentDestination.position, PrinterMachineInstruction.MovementTypes.Absolute);
                        break;

                    case PrintLevelingData.LevelingSystem.Probe7PointRadial:
                        lineBeingSent = LevelWizard7PointRadial.ApplyLeveling(lineBeingSent, currentDestination.position, PrinterMachineInstruction.MovementTypes.Absolute);
                        break;

                    case PrintLevelingData.LevelingSystem.Probe13PointRadial:
                        lineBeingSent = LevelWizard13PointRadial.ApplyLeveling(lineBeingSent, currentDestination.position, PrinterMachineInstruction.MovementTypes.Absolute);
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }

            return lineBeingSent;
        }

    }
}
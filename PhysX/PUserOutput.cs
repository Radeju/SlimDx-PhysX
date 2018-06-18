using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StillDesign.PhysX;

namespace PhysX
{
    public class PUserOutput: UserOutputStream
    {
        public override void Print(string message)
        {
            Console.WriteLine(message);
        }

        public override AssertResponse ReportAssertionViolation(string message, string file, int lineNumber)
        {
            Console.WriteLine(message);
            return AssertResponse.Continue;
        }

        public override void ReportError(ErrorCode errorCode, string message, string file, int lineNumber)
        {
            Console.WriteLine(message);
        }
    }
}

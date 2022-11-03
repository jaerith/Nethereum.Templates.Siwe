using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleProjectSiwe.SiweRecap
{
    public class SiweRecapException: Exception
    {
        public SiweRecapException(string message) : base(message)
        { }
    }
}

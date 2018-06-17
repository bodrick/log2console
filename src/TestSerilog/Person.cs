using System.Collections.Generic;
using Serilog;

namespace SerilogTest
{
    internal class Person
    {
        private readonly ILogger Log = Serilog.Log.ForContext<Person>();
        private string property1;

        public Person(string name, string ssn, string prop1)
        {
            Name = name;
            Ssn = ssn;
            property1 = prop1;
            Log.Verbose("ctor {@Person}", this);
        }

        public string Name { get; set; }
        private string Ssn { get; }

        public List<Address> Addresses { get; set; }
    }
}
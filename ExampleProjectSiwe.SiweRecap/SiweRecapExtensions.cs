using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Nethereum.Siwe.Core;

namespace ExampleProjectSiwe.SiweRecap
{
    using CapabilityMapping = Dictionary<SiweNamespace, SiweRecapCapability>;

    public static class SiweRecapExtensions
    {
        public static void InitRecap(this SiweMessage msg, CapabilityMapping capabilites, string delegateUri)
        {
            msg.InitRecapStatement(capabilites, delegateUri);

            msg.InitRecapResources(capabilites, delegateUri);
        }

        public static void InitRecapStatement(this SiweMessage msg, CapabilityMapping capabilites, string delegateUri)
        {
            var lineNum = 0;

            StringBuilder recapStatementBuilder =
                new StringBuilder("I further authorize " + delegateUri +
                                  " to perform the following actions on my behalf:");

            foreach (var siweNamespace in capabilites.Keys)
            {
                var capability = capabilites[siweNamespace];

                capability
                    .ToStatementText(siweNamespace)
                    .ToList()
                    .ForEach(actionStmt =>
                             recapStatementBuilder.Append(string.Format(" ({0}) {1}", lineNum, actionStmt)));
            }

            msg.Statement = recapStatementBuilder.ToString();
        }

        public static void InitRecapResources(this SiweMessage msg, CapabilityMapping capabilites, string delegateUri)
        {
            // NOTE: Do work here
        }
    }
}

using System.Collections.Generic;

using Nethereum.Siwe.Core;

namespace ExmpleProjectSiwe.SiweRecap
{
    public class SiweRecapCapability : SiweMessage
    {
        public HashSet<string> DefaultActions { get; protected set; }

        public Dictionary<string, HashSet<string>> TargetedActions { get; protected set; }

        public Dictionary<string, string> ExtraFields { get; protected set; }

        public SiweRecapCapability(HashSet<string> defaultActions,
               Dictionary<string, HashSet<string>> targetedActions,
                        Dictionary<string, string> extraFields)
        {
            DefaultActions  = defaultActions;
            TargetedActions = targetedActions;
            ExtraFields     = extraFields;
        }
    }
}
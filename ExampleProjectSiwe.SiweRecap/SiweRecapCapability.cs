using System.Collections.Generic;

namespace ExmpleProjectSiwe.SiweRecap
{
    public class SiweRecapCapability
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

        public bool HasPermissionByTarget(string target, string action)
        {
            HashSet<string>? targetActions = null;

            return TargetedActions.TryGetValue(target, out targetActions) &&
                   (HasPermissionByDefault(action) || targetActions.Any(x => x.ToLower() == action.ToLower()));
        }

        public bool HasPermissionByDefault(string action)
        {
            return DefaultActions.Any(x => x.ToLower() == action.ToLower());
        }


    }
}
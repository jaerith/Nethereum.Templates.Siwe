using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace ExampleProjectSiwe.SiweRecap
{
    using NamespaceActionsMap = Dictionary<string, HashSet<string>>;

    public class SiweRecapCapability
    {
        public const string DefaultTarget = "any";

        private readonly HashSet<string> _defaultActions;

        private readonly NamespaceActionsMap _targetedActions;

        private readonly Dictionary<string, string> _extraFields;

        public HashSet<string> DefaultActions { get { return _defaultActions; } }

        public NamespaceActionsMap TargetedActions { get { return _targetedActions; } }

        public SiweRecapCapability(HashSet<string> defaultActions,
                               NamespaceActionsMap targetedActions,
                        Dictionary<string, string> extraFields)
        {
            _defaultActions  = defaultActions;
            _targetedActions = targetedActions;
            _extraFields     = extraFields;
        }

        public string Encode()
        {
            string jsonCapability = JsonSerializer.Serialize(this);

            return Convert.ToBase64String(Encoding.ASCII.GetBytes(jsonCapability));
        }

        public bool HasPermissionByTarget(string target, string action)
        {
            HashSet<string>? targetActions = null;

            return _targetedActions.TryGetValue(target, out targetActions) &&
                   (HasPermissionByDefault(action) || targetActions.Any(x => x.ToLower() == action.ToLower()));
        }

        public bool HasPermissionByDefault(string action)
        {
            return _defaultActions.Any(x => x.ToLower() == action.ToLower());
        }

        public HashSet<string> ToStatementText(SiweNamespace siweNamespace)
        {            
            var capabilityTextLines = new HashSet<string>();

            capabilityTextLines.Add(FormatDefaultActions(siweNamespace, _defaultActions));

            foreach (var target in _targetedActions.Keys)
            {
                capabilityTextLines.Add(FormatActions(siweNamespace, target, _targetedActions[target]));
            }

            return capabilityTextLines;
        }

        #region Static Methods

        static public string FormatDefaultActions(SiweNamespace siweNamespace, HashSet<string> actions)
        {
            return FormatActions(siweNamespace, DefaultTarget, actions);
        }

        static public string FormatActions(SiweNamespace siweNamespace, string target, HashSet<string> actions)
        {
            return string.Format("{0}: {1} for {2}.", siweNamespace, string.Join(", ", actions), target);
        }

        #endregion 
    }
}
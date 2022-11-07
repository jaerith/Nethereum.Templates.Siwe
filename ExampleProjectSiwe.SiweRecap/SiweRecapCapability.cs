using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text;

namespace ExampleProjectSiwe.SiweRecap
{
    public class SiweRecapCapability
    {
        private readonly HashSet<string> _defaultActions;

        private readonly Dictionary<SiweNamespace, HashSet<string>> _targetedActions;

        private readonly Dictionary<string, string> _extraFields;

        public HashSet<string> DefaultActions { get { return _defaultActions; } }

        public Dictionary<SiweNamespace, HashSet<string>> TargetedActions { get { return _targetedActions; } }

        public SiweRecapCapability(HashSet<string> defaultActions,
        Dictionary<SiweNamespace, HashSet<string>> targetedActions,
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

        public bool HasPermissionByTarget(SiweNamespace siweNamspace, string action)
        {
            HashSet<string>? targetActions = null;

            return _targetedActions.TryGetValue(siweNamspace, out targetActions) &&
                   (HasPermissionByDefault(action) || targetActions.Any(x => x.ToLower() == action.ToLower()));
        }

        public bool HasPermissionByDefault(string action)
        {
            return _defaultActions.Any(x => x.ToLower() == action.ToLower());
        }

        public HashSet<string> ToStatementText(SiweNamespace siweNamespace)
        {
            var textSectionBuilder      = new StringBuilder();
            var capabilityTextLines     = new HashSet<string>();
            var defaultActionsFormatted = new HashSet<string>();
            var actionsFormatted        = new HashSet<string>();

            _defaultActions
                .ToList()
                .ForEach(x => defaultActionsFormatted.Append(FormatAction(siweNamespace, x)));

            foreach (var siweNamespaceKey in _targetedActions.Keys)
            {
                _targetedActions[siweNamespaceKey]
                    .ToList()
                    .ForEach(x => actionsFormatted.Append(FormatAction(siweNamespaceKey, x)));
            }

            if (defaultActionsFormatted.Count > 0)
            {
                capabilityTextLines.Add(String.Join(", ", defaultActionsFormatted));
            }

            if (actionsFormatted.Count > 0)
            {
                capabilityTextLines.Add(String.Join(", ", actionsFormatted));
            }

            return capabilityTextLines;
        }

        #region Static Methods

        static public string FormatAction(SiweNamespace siweNamespace, string defaultAction)
        {
            return string.Format("{0}: {1} for any.", siweNamespace, defaultAction);
        }

        #endregion 
    }
}
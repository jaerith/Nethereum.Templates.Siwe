using System.Collections.Generic;
using System.Text;

namespace ExampleProjectSiwe.SiweRecap
{
    public class SiweRecapCapability
    {
        private readonly HashSet<string> _DefaultActions;

        private readonly Dictionary<SiweNamespace, HashSet<string>> _TargetedActions;

        private readonly Dictionary<string, string> _ExtraFields;

        public SiweRecapCapability(HashSet<string> defaultActions,
        Dictionary<SiweNamespace, HashSet<string>> targetedActions,
                        Dictionary<string, string> extraFields)
        {
            _DefaultActions  = defaultActions;
            _TargetedActions = targetedActions;
            _ExtraFields     = extraFields;
        }

        public bool HasPermissionByTarget(SiweNamespace siweNamspace, string action)
        {
            HashSet<string>? targetActions = null;

            return _TargetedActions.TryGetValue(siweNamspace, out targetActions) &&
                   (HasPermissionByDefault(action) || targetActions.Any(x => x.ToLower() == action.ToLower()));
        }

        public bool HasPermissionByDefault(string action)
        {
            return _DefaultActions.Any(x => x.ToLower() == action.ToLower());
        }

        public HashSet<string> ToStatementText(SiweNamespace siweNamespace)
        {
            var textSectionBuilder      = new StringBuilder();
            var capabilityTextLines     = new HashSet<string>();
            var defaultActionsFormatted = new HashSet<string>();
            var actionsFormatted        = new HashSet<string>();

            _DefaultActions
                .ToList()
                .ForEach(x => defaultActionsFormatted.Append(FormatAction(siweNamespace, x)));

            foreach (var siweNamespaceKey in _TargetedActions.Keys)
            {
                _TargetedActions[siweNamespaceKey]
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
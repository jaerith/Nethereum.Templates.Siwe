﻿using System;
/*
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
*/

using Nethereum.Siwe.Core;

namespace ExampleProjectSiwe.SiweRecap
{
    using CapabilityMap     = Dictionary<SiweNamespace, SiweRecapCapability>;
    using CapabilitySeedMap = Dictionary<SiweNamespace, SiweRecapCapabilitySeed>;

    public class SiweRecapMsgBuilder
    {
        private CapabilitySeedMap _capabilitySeedMap = new CapabilitySeedMap();

        private SiweMessage _siweMessage = new SiweMessage();

        private SiweRecapMsgBuilder(SiweMessage siweMessage)
        {
            _capabilitySeedMap = new CapabilitySeedMap();
            _siweMessage       = siweMessage;
        }

        public static SiweRecapMsgBuilder Init(SiweMessage siweMessage)
        {
            return new SiweRecapMsgBuilder(siweMessage);
        }

        public SiweRecapMsgBuilder AddDefaultActions(SiweNamespace siweNamespace, HashSet<string> defaultActions)
        {
            GetCapabilitySeed(siweNamespace).DefaultActions = defaultActions;
            
            return this;
        }

        public SiweRecapMsgBuilder AddTargetActions(SiweNamespace siweNamespace, string target, HashSet<string> actions)
        {
            GetCapabilitySeed(siweNamespace).TargetedActions[target] = actions;

            return this;
        }

        public SiweMessage Build()
        {
            CapabilityMap capabilityMap = new CapabilityMap();

            _capabilitySeedMap
                .ToList()
                .ForEach(x => capabilityMap[x.Key] = new SiweRecapCapability(x.Value.DefaultActions
                                                                             , x.Value.TargetedActions
                                                                             , new Dictionary<string, string>()));

            return _siweMessage.InitRecap(capabilityMap, _siweMessage.Uri);
        }

        private SiweRecapCapabilitySeed GetCapabilitySeed(SiweNamespace siweNamespace)
        {
            if (!_capabilitySeedMap.ContainsKey(siweNamespace))
            {
                _capabilitySeedMap[siweNamespace] = new SiweRecapCapabilitySeed();
            }

            return _capabilitySeedMap[siweNamespace];
        }
    }

}

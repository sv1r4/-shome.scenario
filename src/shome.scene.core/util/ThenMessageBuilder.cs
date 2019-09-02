using System;
using System.Collections.Generic;
using shome.scene.core.contract;
using shome.scene.core.model;

namespace shome.scene.core.util
{
    public class ThenMessageBuilder
    {
        private string _rawMessage;
        private IReadOnlyList<(SceneConfig.SceneIf SceneIf, IfStatus Status)> _triggersState;
        
        public string Build()
        {
            #region regular return what defined in message
            if (!_rawMessage.StartsWith(Specials.Key))
            {
                return _rawMessage;
            }
            #endregion
            #region process specials - proxy from 'if'
            if (TryGetProxiedMessage(Specials.ProxyRaw,s=>s.RawMessage, out var rawMessage)) return rawMessage;
            if (TryGetProxiedMessage(Specials.Proxy,s=>s.EventValue, out rawMessage)) return rawMessage;
            #endregion
            throw new InvalidOperationException($"Unsupported @special message '{_rawMessage}'");

        }

        private bool TryGetProxiedMessage(string prefix,Func<IfStatus, string> getMessageFunc, out string rawMessage)
        {
            if (_rawMessage.StartsWith(prefix))
            {
                //if proxy index specified in message i.e. '@proxy0' '@proxy 1', '@proxy 2'
                //or for raw message proxy i.e. '@proxy_raw0' '@proxy_raw 1', '@proxy_raw 2'
                //get message from corresponding 'if' otherwise from first one
                if (!int.TryParse(_rawMessage.Remove(0, prefix.Length).Trim(), out var proxyIndex))
                {
                    proxyIndex = 0;
                }

                //check interval
                if (proxyIndex >= 0 && proxyIndex < _triggersState.Count)
                {
                    {
                        rawMessage = getMessageFunc(_triggersState[proxyIndex].Status);
                        return true;
                    }
                }

                throw new IndexOutOfRangeException(
                    $"Specified index={proxyIndex} ('{_rawMessage}') is out of range 'If' collection size [{0}..{_triggersState.Count - 1}]");
            }

            rawMessage = _rawMessage;
            return false;
        }

        public ThenMessageBuilder WithRawMessage(string rawMessage)
        {
            _rawMessage = rawMessage;
            return this;
        }

        public ThenMessageBuilder WithTriggersState(IReadOnlyList<(SceneConfig.SceneIf SceneIf, IfStatus Status)> triggersState)
        {
            _triggersState = triggersState;
            return this;
        }
    }
}

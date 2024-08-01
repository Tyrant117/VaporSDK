using UnityEngine;
using Vapor.Keys;

namespace Vapor.GameplayTag
{
    [DatabaseKeyValuePair]
    public class GameplayTagSo : NamedKeySo
    {
        public KeyDropdownValue Parent;
    }
}

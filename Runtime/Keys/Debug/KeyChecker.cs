using UnityEngine;
using Vapor;
using VaporInspector;

namespace VaporKeys
{
    public class KeyChecker : MonoBehaviour
    {
        [InlineButton("Check", "Check")]
        public string Name;
        public ushort Key;

        private void Check()
        {
            Key = Name.GetStableHashU16();
        }
    }
}

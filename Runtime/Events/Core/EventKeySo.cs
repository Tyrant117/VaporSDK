using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vapor.Keys;

namespace Vapor.Events
{
    /// <summary>
    /// A scriptable object implementation of the IKey interface that derives its display name from the <see cref="ScriptableObject.name"/> and is used with functionality relating to the <see cref="EventBus"/>
    /// </summary>
    [KeyOptions(category: EventKeyUtility.EVENTS_CATEGORY_NAME)]
    public class EventKeySo : NamedKeySo
    {

    }
}

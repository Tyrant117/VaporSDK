namespace Vapor.Blueprints
{
    public abstract class PlayModeIteratorNode : PlayModeNodeBase
    {
        protected bool Looping;

        public virtual void Break()
        {
            Looping = false;
        }
    }
}
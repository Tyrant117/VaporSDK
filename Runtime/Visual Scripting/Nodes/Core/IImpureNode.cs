namespace Vapor.VisualScripting
{
    public interface IImpureNode : INode
    {
        //IImpureNode Previous { get; }
        IImpureNode Next { get; set; }

        void Invoke(IGraphOwner owner);
    }
}

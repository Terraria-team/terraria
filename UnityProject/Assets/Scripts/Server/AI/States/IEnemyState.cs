namespace Server.AI
{
    public interface IEnemyState
    {
        void EnterState();
        void UpdateState();
        void ExitState();
    }
}
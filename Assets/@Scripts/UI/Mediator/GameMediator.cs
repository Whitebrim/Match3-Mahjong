using Core.Infrastructure.StateMachine.States;

namespace UI.Mediator
{
    public class GameMediator : Mediator
    {
        public void GameOver()
        {
            ((GameState)StateMachine.CurrentState).GameOver();
        }
    }
}
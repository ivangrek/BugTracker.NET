/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Changing
{
    using Results;

    public sealed class TransactionCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> commandHandler;
        private readonly IUnitOfWork unitOfWork;

        public TransactionCommandHandlerDecorator(
            ICommandHandler<TCommand> commandHandler,
            IUnitOfWork unitOfWork)
        {
            this.commandHandler = commandHandler;
            this.unitOfWork = unitOfWork;
        }

        public void Handle(TCommand command, out ICommandResult commandResult)
        {
            this.commandHandler
                .Handle(command, out commandResult);

            this.unitOfWork
                .Commit();
        }
    }
}
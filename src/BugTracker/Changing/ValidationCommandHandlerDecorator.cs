/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Changing
{
    using System;
    using FluentValidation;
    using Results;

    public sealed class ValidationCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> commandHandler;
        private readonly IValidator<TCommand> commandValidator;

        public ValidationCommandHandlerDecorator(
            ICommandHandler<TCommand> commandHandler,
            IValidator<TCommand> commandValidator)
        {
            this.commandHandler = commandHandler;
            this.commandValidator = commandValidator;
        }

        public void Handle(TCommand command, out ICommandResult commandResult)
        {
            var validationResult = this.commandValidator
                .Validate(command);

            if (!validationResult.IsValid)
            {
                var builder = CommandResult.Fail();

                foreach (var error in validationResult.Errors)
                    builder.WithError(error.PropertyName, error.ErrorMessage);

                commandResult = builder.Build();

                return;
            }

            try
            {
                this.commandHandler
                    .Handle(command, out commandResult);
            }
            catch (InvalidOperationException ex)
            {
                commandResult = CommandResult.Fail()
                    .WithError(ex.Message)
                    .Build();
            }
        }
    }
}
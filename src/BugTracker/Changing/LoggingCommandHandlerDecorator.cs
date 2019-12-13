/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Changing
{
    using System;
    using System.IO;
    using System.Linq;
    using Results;

    public sealed class LoggingCommandHandlerDecorator<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly ICommandHandler<TCommand> commandHandler;

        public LoggingCommandHandlerDecorator(
            ICommandHandler<TCommand> commandHandler)
        {
            this.commandHandler = commandHandler;
        }

        public void Handle(TCommand command, out ICommandResult commandResult)
        {
            try
            {
                this.commandHandler
                    .Handle(command, out commandResult);
            }
            catch (ArgumentNullException)
            {
                commandResult = CommandResult.Fail()
                    .WithError("System error.")
                    .Build();
            }
            catch (Exception)
            {
                commandResult = CommandResult.Fail()
                    .WithError("System error.")
                    .Build();
            }

            using (var file = new StreamWriter(@"C:\projects\logs.txt", true))
            {
                var commandType = this.commandHandler.GetType().GetGenericArguments()[0];

                file.WriteLine(
                    $"{DateTime.Now:dd/MM/yyyy HH:mm} {commandType.FullName.Split('.')[commandType.FullName.Split('.').Length - 3]}:{commandType.Name.Replace("Command", string.Empty).Substring(1)} => {commandResult.GetType().Name.Replace("CommandResult", string.Empty)}");

                var failCommandResult = commandResult as IFailCommandResult;

                if (failCommandResult != null)
                {
                    var failErrors = failCommandResult.Errors
                        .Where(x => string.IsNullOrEmpty(x.Property))
                        .ToArray();

                    foreach (var failError in failErrors) file.WriteLine($"    - {failError.Message}");

                    if (failErrors.Any()) file.WriteLine(string.Empty);
                }

                foreach (var property in command.GetType().GetProperties())
                {
                    file.WriteLine($"    {property.Name} = {property.GetValue(command)}");

                    if (failCommandResult != null)
                    {
                        var failErrors = failCommandResult.Errors
                            .Where(x => x.Property == property.Name)
                            .ToArray();

                        foreach (var failError in failErrors) file.WriteLine($"        - {failError.Message}");
                    }
                }

                file.WriteLine(string.Empty);
            }
        }
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Changing.Results
{
    using System.Collections.Generic;
    using System.Linq;

    public interface IFailError
    {
        string Property { get; }

        string Message { get; }
    }

    public interface IFailCommandResult : ICommandResult
    {
        IReadOnlyCollection<IFailError> Errors { get; }
    }

    internal sealed class FailCommandResult : IFailCommandResult
    {
        internal IList<IFailError> Errors { get; } = new List<IFailError>();

        IReadOnlyCollection<IFailError> IFailCommandResult.Errors => Errors.ToArray();
    }

    public interface IFailCommandResultBuilder
    {
        IFailCommandResultBuilder WithError(string error);

        IFailCommandResultBuilder WithError(string property, string error);

        ICommandResult Build();
    }

    internal sealed class FailCommandResultBuilder : IFailCommandResultBuilder
    {
        private readonly FailCommandResult result;

        public FailCommandResultBuilder(FailCommandResult result)
        {
            this.result = result;
        }

        public IFailCommandResultBuilder WithError(string message)
        {
            this.result.Errors.Add(new FailError(message));

            return this;
        }

        public IFailCommandResultBuilder WithError(string property, string message)
        {
            this.result.Errors.Add(new FailError(property, message));

            return this;
        }

        public ICommandResult Build()
        {
            return this.result;
        }

        private sealed class FailError : IFailError
        {
            public FailError(string message)
            {
                Property = string.Empty;
                Message = message;
            }

            public FailError(string property, string message)
            {
                Property = property;
                Message = message;
            }

            public string Property { get; }

            public string Message { get; }
        }
    }
}
/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Identification.Changing.Validators
{
    using Commands;
    using FluentValidation;

    internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
    }
}
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using System.Reflection;

namespace BackPredictFinance.API.Data
{
    public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
    {
        private readonly IStringLocalizer _localizer;

        public LocalizedIdentityErrorDescriber(IStringLocalizerFactory factory)
        {
            var type = typeof(LocalizedIdentityErrorDescriber);
            _localizer = factory.Create("IdentityErrorMessages", Assembly.GetExecutingAssembly().GetName().Name);
        }

        public override IdentityError PasswordTooShort(int length)
            => new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = _localizer[nameof(PasswordTooShort), length]
            };

        public override IdentityError PasswordRequiresDigit()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = _localizer[nameof(PasswordRequiresDigit)]
            };

        public override IdentityError PasswordRequiresUpper()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = _localizer[nameof(PasswordRequiresUpper)]
            };
    }


}

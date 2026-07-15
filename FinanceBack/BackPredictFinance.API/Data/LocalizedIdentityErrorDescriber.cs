using Microsoft.AspNetCore.Identity;

namespace BackPredictFinance.API.Data
{
    /// <summary>
    /// Traduit en français les erreurs Identity exposées à l'utilisateur (règles
    /// de mot de passe, doublons). Messages en dur pour rester cohérent avec les
    /// CustomException du reste de l'API et éviter une dépendance resx.
    /// </summary>
    public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError PasswordTooShort(int length)
            => new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = $"Le mot de passe doit comporter au moins {length} caractères."
            };

        public override IdentityError PasswordRequiresDigit()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = "Le mot de passe doit contenir au moins un chiffre."
            };

        public override IdentityError PasswordRequiresUpper()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = "Le mot de passe doit contenir au moins une majuscule."
            };

        public override IdentityError PasswordRequiresLower()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = "Le mot de passe doit contenir au moins une minuscule."
            };

        public override IdentityError PasswordRequiresNonAlphanumeric()
            => new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description = "Le mot de passe doit contenir au moins un caractère spécial."
            };

        public override IdentityError DuplicateEmail(string email)
            => new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = "Un compte existe déjà pour cet email."
            };

        public override IdentityError DuplicateUserName(string userName)
            => new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = "Un compte existe déjà pour cet identifiant."
            };
    }
}

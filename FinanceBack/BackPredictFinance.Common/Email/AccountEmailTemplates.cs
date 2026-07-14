namespace BackPredictFinance.Common.Email
{
    /// <summary>
    /// Fabrique les gabarits HTML des emails liés au compte (confirmation, etc.).
    /// </summary>
    public static class AccountEmailTemplates
    {
        /// <summary>
        /// Construit le corps HTML de l'email de confirmation d'adresse.
        /// </summary>
        public static string BuildEmailConfirmationHtml(string link)
        {
            return $@"
                <html>
                  <body style=""font-family: Arial, sans-serif; font-size: 14px; color: #333333; background-color: #f5f5f5; margin: 0; padding: 0;"">
                    <table width=""100%"" cellpadding=""0"" cellspacing=""0"">
                      <tr>
                        <td align=""center"" style=""padding: 30px 15px;"">
                          <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; overflow: hidden;"">
                            <tr>
                              <td style=""background-color: #0d6efd; color: #ffffff; padding: 16px 24px; font-size: 18px; font-weight: bold;"">
                                PredictFinance
                              </td>
                            </tr>
                            <tr>
                              <td style=""padding: 24px;"">
                                <p>Bonjour,</p>
                                <p>Confirmez votre adresse email pour activer votre compte PredictFinance.</p>
                                <p style=""text-align: center; margin: 30px 0;"">
                                  <a href=""{link}""
                                     style=""display: inline-block; padding: 12px 24px; background-color: #0d6efd; color: #ffffff;
                                            text-decoration: none; border-radius: 4px; font-weight: bold;"">
                                    Confirmer mon email
                                  </a>
                                </p>
                                <p>
                                  Si le bouton ne fonctionne pas, copiez-collez le lien suivant dans votre navigateur&nbsp;:<br/>
                                  <a href=""{link}"">{link}</a>
                                </p>
                                <p style=""font-size: 12px; color: #666666; margin-top: 24px;"">
                                  Si vous n'êtes pas à l'origine de cette inscription, vous pouvez ignorer cet e-mail.
                                </p>
                              </td>
                            </tr>
                            <tr>
                              <td style=""background-color: #f0f0f0; padding: 16px 24px; font-size: 12px; color: #777777; text-align: center;"">
                                Cet e-mail a été envoyé automatiquement, merci de ne pas y répondre.
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>";
        }
    }
}

/**
 * Règle de mot de passe alignée sur la configuration Identity backend
 * (Program.cs) : minimum 6 caractères, au moins une minuscule, une majuscule
 * et un chiffre. La prévalidation front évite les 422 « surprise ».
 */
export const STRONG_PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{6,}$/;

export const STRONG_PASSWORD_HINT = '6 caractères minimum, avec au moins une majuscule, une minuscule et un chiffre.';

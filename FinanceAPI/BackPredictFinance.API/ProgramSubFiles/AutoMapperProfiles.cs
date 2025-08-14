using System;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;

namespace BackPredictFinance.API.ProgramSubFiles
{
    public static class AutoMapperProfiles
    {
        /// <summary>
        /// Configure AutoMapper en scannant tous les profils dans l'assembly spécifié
        /// </summary>
        public static void SetAutoMapper(IServiceCollection services)
        {
            // Utilise l'extension AddAutoMapper pour enregistrer tous les profils
            // services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        }
    }
}

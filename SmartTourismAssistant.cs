using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TesteLLMs.Interfaces;
using TesteLLMs.Models;

namespace TesteLLMs
{
    public class SmartTourismAssistant
    {
        private readonly IAIService _AI;
        private readonly TourismDataService _dataService;

        public SmartTourismAssistant(string apiKey, string datasetPath)
        {
            _AI = AIServiceFactory.CreateService(apiKey, datasetPath);
            _dataService = new TourismDataService(datasetPath);
        }

        public async Task<string> GetTravelRecommendation(string query)
        {
            // Buscar dados relevantes
            var relevantDestinations = await _dataService.SearchDestinations(query);

            if (!relevantDestinations.Any())
            {
                return "Desculpe, não encontrei informações sobre este destino na minha base de dados.";
            }

            // Construir contexto
            var context = await BuildTravelContext(relevantDestinations);

            var prompt = $"""
        CONTEXTO - DADOS TURÍSTICOS:
        {context}

        PERGUNTA DO USUÁRIO: {query}

        INSTRUÇÕES:
        - Baseie sua resposta APENAS nos dados fornecidos acima
        - Seja útil e informativo
        - Mantenha a resposta natural e envolvente
        - Se não tiver informação específica, indique isso claramente
        - Inclua detalhes práticos como melhor época, custos, segurança

        RESPOSTA:
        """;

            return await _AI.GenerateResponse(prompt);
        }

        public async Task<string> GenerateTravelItinerary(string destination, int days, string preferences = "")
        {
            var destInfo = await _dataService.GetDestinationInfo(destination);

            if (destInfo == null)
            {
                return $"Desculpe, não tenho informações sobre {destination} na minha base de dados.";
            }

            var prompt = $"""
        DADOS SOBRE {destination.ToUpper()}:
        - País: {destInfo.Country}
        - Região: {destInfo.Region}
        - Categoria: {destInfo.Category}
        - Melhor época: {destInfo.BestTimeToVisit}
        - Custo de vida: {destInfo.CostOfLiving}
        - Segurança: {destInfo.Safety}
        - Comidas típicas: {destInfo.FamousFoods}
        - Descrição: {destInfo.Description}

        CRIE UM ROTEIRO DE {days} DIAS:
        - Preferências: {(!string.IsNullOrEmpty(preferences) ? preferences : "Nenhuma preferência específica")}

        Estruture o roteiro com:
        1. Atividades por período (manhã, tarde, noite)
        2. Inclua atrações baseadas na categoria do destino
        3. Sugira experiências culturais e gastronômicas
        4. Dicas práticas baseadas nos dados acima

        ROTEIRO:
        """;

            return await _AI.GenerateResponse(prompt);
        }

        public async Task<string> CompareDestinations(string destination1, string destination2)
        {
            var dest1 = await _dataService.GetDestinationInfo(destination1);
            var dest2 = await _dataService.GetDestinationInfo(destination2);

            if (dest1 == null || dest2 == null)
            {
                return "Desculpe, não encontrei um ou ambos os destinos na base de dados.";
            }

            var prompt = $"""
        COMPARAÇÃO ENTRE DESTINOS:

        {destination1.ToUpper()}:
        - País: {dest1.Country}
        - Região: {dest1.Region}
        - Categoria: {dest1.Category}
        - Melhor época: {dest1.BestTimeToVisit}
        - Custo: {dest1.CostOfLiving}
        - Segurança: {dest1.Safety}
        - Descrição: {dest1.Description}

        {destination2.ToUpper()}:
        - País: {dest2.Country}
        - Região: {dest2.Region}
        - Categoria: {dest2.Category}
        - Melhor época: {dest2.BestTimeToVisit}
        - Custo: {dest2.CostOfLiving}
        - Segurança: {dest2.Safety}
        - Descrição: {dest2.Description}

        Faça uma comparação detalhada entre os dois destinos, destacando:
        - Vantagens de cada um
        - Perfil de viajante mais adequado
        - Custo-benefício
        - Experiências únicas de cada destino

        COMPARAÇÃO:
        """;

            return await _AI.GenerateResponse(prompt);
        }

        private async Task<string> BuildTravelContext(List<TourismDestination> destinations)
        {
            var context = new StringBuilder();

            foreach (var dest in destinations.Take(3))
            {
                context.AppendLine($"🏛️ **{dest.Destination}**");
                context.AppendLine($"📍 {dest.Country} | {dest.Region} | {dest.Category}");
                context.AppendLine($"📅 Melhor época: {dest.BestTimeToVisit}");
                context.AppendLine($"💰 Custo: {dest.CostOfLiving}");
                context.AppendLine($"🛡️ Segurança: {dest.Safety}");
                context.AppendLine($"🍽️ Comidas: {dest.FamousFoods}");
                context.AppendLine($"📖 {dest.Description}");
                context.AppendLine();
            }

            return context.ToString();
        }
    }
}

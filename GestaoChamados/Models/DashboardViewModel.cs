using System.Collections.Generic;

namespace GestaoChamados.Models
{
    public class RankingModel
    {
        public string Nome { get; set; }
        public int Contagem { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalChamados { get; set; }
        public int ChamadosAbertos { get; set; }
        public int ChamadosEmAtendimento { get; set; }
        public int ChamadosResolvidos { get; set; }
        public int ChamadosNaFila { get; set; }

        // Dados do sistema (gráfico geral)
        public List<string> StatusLabels { get; set; } = new List<string>();
        public List<int> StatusCounts { get; set; } = new List<int>();

        // **NOVO: Dados específicos do técnico**
        public List<string> MeuStatusLabels { get; set; } = new List<string>();
        public List<int> MeuStatusCounts { get; set; } = new List<int>();
        public double PercentualResolvidos { get; set; }
        public double NotaMediaSatisfacao { get; set; }
        public int TotalAvaliacoes { get; set; }

        public List<RankingModel> TopUsuarios { get; set; } = new List<RankingModel>();
        public List<RankingModel> TopTecnicos { get; set; } = new List<RankingModel>();
    }
}
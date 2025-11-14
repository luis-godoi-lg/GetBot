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

        public List<string> StatusLabels { get; set; } = new List<string>();
        public List<int> StatusCounts { get; set; } = new List<int>();

        public List<RankingModel> TopUsuarios { get; set; } = new List<RankingModel>();
        public List<RankingModel> TopTecnicos { get; set; } = new List<RankingModel>();

        // ADICIONE A PROPRIEDADE QUE ESTAVA FALTANDO AQUI
        public int ChamadosNaFila { get; set; }
    }
}
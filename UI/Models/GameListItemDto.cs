public class GameListItemDTO
    {
        public string Id { get; set; } = string.Empty;
        public string ListId { get; set; } = string.Empty;
        public Guid GameId { get; set; } 
        public string Notes { get; set; } = string.Empty;
        public int Order { get; set; }
    }
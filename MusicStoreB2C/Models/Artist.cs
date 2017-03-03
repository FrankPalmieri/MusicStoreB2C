using System.ComponentModel.DataAnnotations;

namespace MusicStoreB2C.Models
{
    public class Artist
    {
        public int ArtistId { get; set; }

        [Required]
        public string Name { get; set; }
    }
}
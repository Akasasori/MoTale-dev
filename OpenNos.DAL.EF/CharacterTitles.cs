using OpenNos.Domain;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OpenNos.DAL.EF
{
    public class CharacterTitles
    {
        [Key]
        public long TitleKey { get; set; }

        public long CharacterId { get; set; }

        public int TitleId { get; set; }
    }
}

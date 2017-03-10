using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicStoreB2C.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Owner { get; set; }
        public string Text { get; set; }
        public bool Completed { get; set; }
        public DateTime DateModified { get; set; }
    }
}

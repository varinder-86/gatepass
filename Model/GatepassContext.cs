using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Gatepaswebapi.Model
{
    public class GatepassContext :DbContext
    {
        public GatepassContext(DbContextOptions<GatepassContext> options) : base(options) { }
        
            public DbSet<Gatepass> Gatepasses { get; set; }
        
    }
}

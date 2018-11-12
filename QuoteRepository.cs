using PureIP.Portal.Domain.Models.Quote;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PureIP.Portal.Data.Quote
{
    public class QuoteRepository : BaseRepository<QuoteContext>, Domain.Data.IQuoteRepository
    {
        public QuoteRepository(IServiceProvider container) : base(container)
        {
        }
    }
}

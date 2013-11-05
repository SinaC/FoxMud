using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FoxMud.Game;
using FoxMud.Game.Item;
using FoxMud.Game.World;

namespace FoxMud
{
    /// <summary>
    /// simple mud. prototype a basic tcp server that accepts multiple connections
    /// and echoes any input received from the connection's prompt
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Mapper.CreateMap<Template, PlayerItem>()
                  .ForMember(dst => dst.TemplateKey, opt => opt.MapFrom(src => src.Key));
            Mapper.CreateMap<MobTemplate, NonPlayer>()
                  .ForMember(dst => dst.Inventory, opt => opt.ResolveUsing<NonPlayerInventoryResolver>())
                  .ForMember(dst => dst.Equipped, opt => opt.ResolveUsing<NonPlayerEquippedResolver>())
                  .ForMember(dst => dst.MobTemplateKey, opt => opt.MapFrom(src => src.Key));

            using (var server = new Server())
            {
                server.Start();
            }
        }
    }
}

﻿using System;

namespace Mipmap
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Mip game = new Mip(600, 400))
            {
                game.Run();
            }
        }
    }
}

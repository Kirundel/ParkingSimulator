﻿using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using static Infrastructure.Constants;

namespace Domain
{
    public class Car
    {
        private readonly IReadOnlyList<Color> ColorsRandomList = new Color[] 
        {
            Colors.Black,
            Colors.Red,
            Colors.RosyBrown
        };

        public Car()
        {
            Coordinates = new Coord(-1, 0);
            CarColor = ColorsRandomList[RND.Next(ColorsRandomList.Count)];
            IsTruck = RND.NextDouble() > 0.7;
        }

        public event Action UnlockParkingSpace;
        public event Action<Car> EntryReached;
        public event Action<Car> ExitReached;

        public Queue<Coord> Way { get; private set; }

        public Coord Coordinates { get; set; }

        public double Step { get; private set; }

        public double AllStep { get; set; }

        public Coord From { get; set; }

        public Coord To { get; set; }

        public bool NeedDelete { get; private set; } = false;

        public double Speed;

        public Color CarColor { get; }

        public bool IsTruck { get; }

        public Coord Entry { get; set; }

        public Coord Exit { get; set; }

        public bool CarOnParking { get; private set; } = false;

        public void GenerateWay(Queue<Coord> way)
        {
            Way = way;
            From = new Coord(-1, 0);
            To = new Coord(1, 0);
        }

        public void Move(double difference)
        {
            //Debug.WriteLine("CAR MOVE");

            while (difference > SMALL_CONSTANT)
            {
                DoMove(Coordinates, ref difference);
                if (Step + SMALL_CONSTANT > 100)
                {
                    if (Coordinates is StopCoord sc)
                        UnlockParkingSpace?.Invoke();
                    if (Way.Count == 0)
                    {
                        NeedDelete = true;
                        break;
                    }
                    else
                    {
                        var prev = Coordinates;
                        var now = Way.Dequeue();
                        var next = Way.FirstOrDefault() ?? new Coord(now.X + 1, now.Y);
                        Coordinates = now;
                        From = new Coord(prev.X - now.X, prev.Y - now.Y);
                        To = new Coord(next.X - now.X, next.Y - now.Y);
                        Step = 0;
                        if (now == Entry && prev.Y == 0)
                        {
                            AllStep = 0;
                            EntryReached?.Invoke(this);
                            CarOnParking = true;
                        }
                        if (prev == Exit && now.Y == 0)
                        {
                            ExitReached?.Invoke(this);
                            CarOnParking = false;
                        }
                    }
                }
            }
        }

        private void DoMove(Coord current, ref double difference)
        {
            double min;

            int stopCoordinate = IsTruck ? 0 : 50; 

            min = Math.Min(difference * Speed, Math.Max(0, stopCoordinate - Step));
            difference -= min / Speed;
            Step += min;

            AllStep += min / Speed;

            if (current is StopCoord stopCoord)
            {
                min = Math.Min(difference, stopCoord.StopTime);
                difference -= min;
                stopCoord.StopTime -= min;

                AllStep += min;
            }

            min = Math.Min(difference * Speed, 100 - Step);
            difference -= min / Speed;
            Step += min;

            AllStep += min;
        }
    }
}

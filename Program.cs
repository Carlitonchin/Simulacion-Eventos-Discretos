using System;
using System.Collections;
using System.Collections.Generic;

namespace Eventos_Discretos
{
    class Program
    {

        public static Random r = new Random();

        public static double U() 
        {
            //distribucion uniforme (0,1)
            return r.NextDouble();
        }

        public static double Uniform(double l0, double lf) 
        {
            //distribucion uniforme (l0, lf)
            return l0 + (lf - l0) * U();
        }

        public static double Time(double h) 
        {
            // distribucion exponencial, lambda = h
            if(h == 0)
                throw new Exception("h must be != 0");

            return -1 * (1 / h) * Math.Log(U());
        }

        public class Kojo 
        {
            private class Worker 
            {
                private List<int> queue;
                private bool thirdPerson;

                public Worker(bool thirdPerson) 
                {
                    this.thirdPerson = thirdPerson;
                    this.queue = new List<int>();
                }
            }

            private double openTime;
            private double closeTime;
            private Tuple<double, double>[] criticalSpans;
            private bool thirdPerson;
            private enum foodType { sandwich, sushi};

            private abstract class Client 
            {
                public double exitTime;
                public double enterTime;
                public Client(double enterTime) 
                {
                    this.enterTime = enterTime;
                    this.exitTime = double.MaxValue;
                }

                public abstract void generateExitTime(double since);
                public static double LessExitTime(List<Client> clients) 
                {
                    double result = double.MaxValue;

                    for (int i = 0; i < clients.Count; i++)
                    {
                        if (result >= clients[i].exitTime)
                            result = clients[i].exitTime;
                    }

                    return result;
                }
            }

            private class SushiClient : Client
            {
                public SushiClient(double enterTime) : base(enterTime) { }
                public override void generateExitTime(double since)
                {
                    this.exitTime = Uniform(5, 8) + since;
                }
            }

            private class SandwichClient : Client
            {
                public SandwichClient(double enterTime) : base(enterTime) { }
                public override void generateExitTime(double since)
                {
                    this.exitTime = Uniform(3, 5) + since;
                }
            }


            public Kojo(double openTime, double closeTime, Tuple<double, double>[] criticalSpans) 
            {
                this.openTime = openTime;
                this.closeTime = closeTime;
                this.criticalSpans = criticalSpans;
            }

            private foodType SandwichOrSushi() 
            {
                // retorna 0 o 1, con un 50% de probabilidad cada uno
                // 0: el cliente quiere sandwich
                // 1: el cliente quire sushi
                return r.Next(2) == 0 ? foodType.sandwich : foodType.sushi;
            }

            private bool isCritical(double time) 
            {
                if (criticalSpans == null)
                    return false;

                foreach (var span in this.criticalSpans)
                {
                    if (time >= span.Item1 && time <= span.Item2)
                        return true;
                }

                return false;
            }

            public List<Tuple<double, double>> Start(double h, double h_in_criticalSpans, bool thirdPerson) 
            {
                List<Client> queue = new List<Client>();
                List<Tuple<double, double>> result = new List<Tuple<double, double>>();
                double time = openTime;
                double lambda = isCritical(time) ? h_in_criticalSpans : h;
                double ta = Time(lambda) + time;
                double td = double.MaxValue;
                int workersBussy = 0;
                Client extraWorkerClients = null;

                while (true) 
                {
                    //simulacion
                    if (ta <= td && ta <= this.closeTime)
                    {
                        //arribo
                        time = ta;
                        // Que va a comer este cliente? sandwich o sushi?
                        foodType sandwichOrSushi = SandwichOrSushi();
                        if (sandwichOrSushi == foodType.sushi)
                            queue.Add(new SushiClient(ta));
                        else
                            queue.Add(new SandwichClient(ta));


                        lambda = isCritical(time) ? h_in_criticalSpans : h;
                        ta = time + Time(lambda); // tiempo del proximo arribo

                        while (workersBussy < 2)
                        {
                            // mientras haya cocineros libres, se le asigna el tiempo de salida en dependencia del pedido
                            var client = queue.Find(c => c.exitTime == double.MaxValue); // retorna el primer pedido sin tiempo de salida

                            if (client == null)
                                break;

                            client.generateExitTime(time);
                            workersBussy++;

                        }

                        if(isCritical(time) && thirdPerson && extraWorkerClients == null) 
                        {
                            extraWorkerClients = queue.Find(c => c.exitTime == double.MaxValue);
                            if(extraWorkerClients != null) 
                            {
                                extraWorkerClients.generateExitTime(time);
                            }
                        }

                        td = Client.LessExitTime(queue);
                    }

                    else if (td < ta)
                    {
                        //evento de salida
                        time = td;
                        var client = queue.Find(c => c.exitTime == td);
                        
                        queue.Remove(client); // cliente coge su pedido
                        if (client != extraWorkerClients)
                            workersBussy--; // cocinero se desocupa
                        else
                            extraWorkerClients = null; // cocinero extra se desocupa

                        result.Add(new Tuple<double, double>(client.enterTime, client.exitTime));

                        if (queue.Count == 0)
                            td = double.MaxValue;

                        else
                        {
                            while (workersBussy < 2)
                            {
                                // mientras haya cocineros libres, se le asigna el tiempo de salida en dependencia del pedido
                                client = queue.Find(c => c.exitTime == double.MaxValue); // retorna el primer pedido sin tiempo de salida

                                if (client == null)
                                    break;

                                client.generateExitTime(time);
                                workersBussy++;
                            }
                            if(isCritical(time) && thirdPerson && extraWorkerClients == null) 
                            {
                                extraWorkerClients = queue.Find(c => c.exitTime == double.MaxValue);
                                if (extraWorkerClients != null)
                                    extraWorkerClients.generateExitTime(time);
                            }
                            td = Client.LessExitTime(queue);
                        }
                    }

                    else if (ta > this.closeTime && queue.Count == 0)
                        break;

                    if (ta > this.closeTime)
                        ta = double.MaxValue;
                }

                return result;
            }
        }

        public static void Run(Kojo kojo, bool thirdPerson) 
        {
            double timeSpan = 0;
            double moreThan5 = 0;
            double comulativeTime = 0;
            int count = 0;
            for (int i = 0; i < 100; i++)
            {
                var result = kojo.Start(0.15, 0.5, thirdPerson);
                count += result.Count;
                foreach (var item in result)
                {
                    var enter = item.Item1;
                    var exit = item.Item2;
                    timeSpan = exit - enter;
                    comulativeTime += timeSpan;
                    if (timeSpan > 5.0)
                    {
                        moreThan5++;
                    }

                }
            }

            Console.WriteLine((thirdPerson)?"Con un 3er trabajador para los horarios pico:":"Sin un tercer trabajador:");
            Console.WriteLine("--> Porcentaje de las personas que esperan mas de 5 minutos: " + (moreThan5 / count));
            Console.WriteLine("--> Tiempo promedio de espera: " + comulativeTime / count);
        }
        static void Main(string[] args)
        {
            var kojo = new Kojo(0, 660, new Tuple<double, double>[] { new Tuple<double, double>(90, 210), new Tuple<double, double>(420, 540)});
            Run(kojo, false);
            Console.WriteLine();
            Run(kojo, true);
           
        }
    }
}

﻿// -----------------------------------------------------------------------
//  <copyright file="Program.cs" company="Asynkron AB">
//      Copyright (C) 2015-2016 Asynkron AB All rights reserved
//  </copyright>
// -----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Proto;
using Proto.Mailbox;
using static Proto.Actor;

public class Program
{
    static void Main(string[] args)
    {
        var context = new RootContext(new ActorSystem());
        Console.WriteLine($"Is Server GC {GCSettings.IsServerGC}");
        const int messageCount = 1_000_000;
        const int batchSize = 100;

        Console.WriteLine("Dispatcher\t\tElapsed\t\tMsg/sec");
        var tps = new[] { 50,100,200,300, 400, 500, 600, 700, 800, 900 };
        foreach (var t in tps)
        {
            var d = new ThreadPoolDispatcher { Throughput = t };

            var clientCount = Environment.ProcessorCount * 1;
            var clients = new PID[clientCount];
            var echos = new PID[clientCount];
            var completions = new TaskCompletionSource<bool>[clientCount];

            var echoProps = Props.FromProducer(() => new EchoActor())
                .WithDispatcher(d)
                .WithMailbox(() => BoundedMailbox.Create(2048));

            for (var i = 0; i < clientCount; i++)
            {
                var tsc = new TaskCompletionSource<bool>();
                completions[i] = tsc;
                var clientProps = Props.FromProducer(() => new PingActor(tsc, messageCount, batchSize))
                    .WithDispatcher(d)
                    .WithMailbox(() => BoundedMailbox.Create(2048));

                clients[i] = context.Spawn(clientProps);
                echos[i] = context.Spawn(echoProps);
            }
            var tasks = completions.Select(tsc => tsc.Task).ToArray();
            var sw = Stopwatch.StartNew();
            for (var i = 0; i < clientCount; i++)
            {
                var client = clients[i];
                var echo = echos[i];

                context.Send(client, new Start(echo));
            }
            Task.WaitAll(tasks);

            sw.Stop();
            var totalMessages = messageCount * 2 * clientCount;

            var x = ((int)(totalMessages / (double)sw.ElapsedMilliseconds * 1000.0d)).ToString("#,##0,,M", CultureInfo.InvariantCulture);
            Console.WriteLine($"{t}\t\t\t{sw.ElapsedMilliseconds} ms\t\t{x}");
            Thread.Sleep(2000);
        }

        Console.ReadLine();
    }
}

public class Msg
{
    public Msg(PID pingActor)
    {
        PingActor = pingActor;
    }

    public PID PingActor { get; }
}

public class Start
{
    public Start(PID sender)
    {
        Sender = sender;
    }

    public PID Sender { get; }
}

public class EchoActor : IActor
{
    public Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Msg msg:
                context.Send(msg.PingActor, msg);
                break;
        }
        return Done;
    }
}


public class PingActor : IActor
{
    private readonly int _batchSize;
    private readonly TaskCompletionSource<bool> _wgStop;

    private int _messageCount;
    private PID _targetPid;

    public PingActor(TaskCompletionSource<bool> wgStop, int messageCount, int batchSize)
    {
        _wgStop = wgStop;
        _messageCount = messageCount;
        _batchSize = batchSize;
    }

    public Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Start s:
                _targetPid = s.Sender;
                SendBatch(context);
                break;
            case Msg m:
                _messageCount--;
                
                context.Send(_targetPid,m);

                if (_messageCount <= 0)
                {
                    _wgStop.SetResult(true);
                }
                break;
        }
        return Done;
    }

    private void SendBatch(IContext context)
    {
        var m = new Msg(context.Self);

        for (var i = 0; i < _batchSize; i++)
        {
            context.Send(_targetPid, m);
        }

        _messageCount -= _batchSize;
    }
}
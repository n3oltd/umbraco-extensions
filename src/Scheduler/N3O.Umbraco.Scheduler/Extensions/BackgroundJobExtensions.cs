using N3O.Umbraco.Mediator;
using N3O.Umbraco.Parameters;
using NodaTime;
using System;

namespace N3O.Umbraco.Scheduler.Extensions;

public static class BackgroundJobExtensions {
    public static void EnqueueCommand<TCommand>(this IBackgroundJob backgroundJob, params object[] parameters)
        where TCommand : Request<None, None> {
        EnqueueCommand<TCommand, None>(backgroundJob, null, parameters);
    }

    public static void EnqueueCommand<TCommand>(this IBackgroundJob backgroundJob,
                                                Action<IFluentParametersBuilder> addParameters,
                                                params object[] parameters)
        where TCommand : Request<None, None> {
        EnqueueCommand<TCommand, None>(backgroundJob, None.Empty, addParameters, parameters);
    }
    
    public static void EnqueueCommand<TCommand, TReq>(this IBackgroundJob backgroundJob,
                                                      TReq req,
                                                      params object[] parameters)
        where TCommand : Request<TReq, None>
        where TReq : class {
        EnqueueCommand<TCommand, TReq>(backgroundJob, req, null, parameters);
    }

    public static void EnqueueCommand<TCommand, TReq>(this IBackgroundJob backgroundJob,
                                                      TReq req,
                                                      Action<IFluentParametersBuilder> addParameters,
                                                      params object[] parameters)
        where TCommand : Request<TReq, None>
        where TReq : class {
        backgroundJob.Enqueue<TCommand, TReq>(GetJobName<TCommand>(parameters), req, addParameters);
    }
    
    public static string GetJobName<TCommand>(params object[] parameters) {
        return $"{typeof(TCommand).Name.Replace("Command", "").Replace("Event", "")}({string.Join(", ", parameters)})";
    }

    public static void ScheduleCommand<TCommand>(this IBackgroundJob backgroundJob,
                                                 Duration fromNow,
                                                 params object[] parameters)
        where TCommand : Request<None, None> {
        ScheduleCommand<TCommand, None>(backgroundJob, fromNow, null, parameters);
    }

    public static void ScheduleCommand<TCommand>(this IBackgroundJob backgroundJob,
                                                 Duration fromNow,
                                                 Action<IFluentParametersBuilder> addParameters,
                                                 params object[] parameters)
        where TCommand : Request<None, None> {
        ScheduleCommand<TCommand, None>(backgroundJob, fromNow, None.Empty, addParameters, parameters);
    }
    
    public static void ScheduleCommand<TCommand, TReq>(this IBackgroundJob backgroundJob,
                                                       Duration fromNow,
                                                       TReq req,
                                                       params object[] parameters)
        where TCommand : Request<TReq, None>
        where TReq : class {
        ScheduleCommand<TCommand, TReq>(backgroundJob, fromNow, req, null, parameters);
    }

    public static void ScheduleCommand<TCommand, TReq>(this IBackgroundJob backgroundJob,
                                                       Duration fromNow,
                                                       TReq req,
                                                       Action<IFluentParametersBuilder> addParameters,
                                                       params object[] parameters)
        where TCommand : Request<TReq, None>
        where TReq : class {
        backgroundJob.Schedule<TCommand, TReq>(GetJobName<TCommand>(parameters), fromNow, req, addParameters);
    }

}
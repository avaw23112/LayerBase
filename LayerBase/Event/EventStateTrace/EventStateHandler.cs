namespace LayerBase.Core.EventStateTrace;

public delegate void EventCompletedGlobalHandler(
    ref EventState state // ref：按引用传参；回调里修改 state 会直接改到调用方那份变量
);


public class EventStateHandler
{
    
}
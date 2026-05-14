using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public interface IJobQueue
    {
        void Push(Action job);
    }
    public class JobQueue : IJobQueue
    {
        Queue<Action> _jobQueue = new Queue<Action>();
        object _lock = new object();
        bool _flush = false;

        public void Push(Action job)
        {
            bool flush = false;

            lock (_lock)
            {
                _jobQueue.Enqueue(job);
                if (_flush == false)
                    flush = _flush = true;
            }

            // Flush()가 오래걸릴 경우를 대비해 이 부분을 락구문에 넣지 않고 여기서 실행
            // 스택에 올라가 있는 지역 변수 flush를 사용하는 이유는 전역 변수 _flush를 사용할 경우
            // 예를 들어 Flush 중인데 다른 스레드들이 Flush()를 실행하러 순서대로 실행 안될 수 있음
            // 딱 하나의 스레드만 실행을 담당하게 위함임
            if (flush)
                Flush();
        }

        void Flush()
        {
            while (true)
            {
                Action action = Pop();
                if (action == null)
                    return;
                action.Invoke();
            }
        }

        Action Pop()
        {
            lock (_lock)
            {
                if (_jobQueue.Count == 0)
                {
                    _flush = false;
                    return null;
                }

                return _jobQueue.Dequeue();
            }
        }
    }
}

using System;
using System.Collections.Generic;
namespace StandUp {
    public enum Phase { Working, Alert, Resting, RestDone }
    // Deadlines, rather than tick counts, preserve elapsed time across sleep/wake.
    public sealed class ReminderClock {
        public Phase State { get; private set; }
        public bool Paused { get; private set; }
        public DateTime Deadline { get; private set; }
        public TimeSpan Duration { get; private set; }
        private TimeSpan frozen;
        public event Action Due;
        public void StartWork(DateTime now, TimeSpan duration) { State=Phase.Working; Paused=false; Duration=duration; Deadline=now+duration; }
        public void StartRest(DateTime now, TimeSpan duration) { State=Phase.Resting; Paused=false; Duration=duration; Deadline=now+duration; }
        public TimeSpan Remaining(DateTime now) {
            if(State==Phase.Alert||State==Phase.RestDone)return TimeSpan.Zero;
            TimeSpan t=Paused?frozen:Deadline-now; return t<TimeSpan.Zero?TimeSpan.Zero:t;
        }
        public void Tick(DateTime now) {
            if(Paused||now<Deadline)return;
            if(State==Phase.Working){State=Phase.Alert;if(Due!=null)Due();}
            else if(State==Phase.Resting)State=Phase.RestDone;
        }
        public void TogglePause(DateTime now) {
            if(State!=Phase.Working&&State!=Phase.Resting)return;
            if(!Paused){frozen=Remaining(now);Paused=true;}
            else{Deadline=now+frozen;Paused=false;}
        }
        public void TestAlert(){State=Phase.Alert;Paused=false;if(Due!=null)Due();}
        public void Snooze(DateTime now){StartWork(now,TimeSpan.FromMinutes(1));}
    }
}

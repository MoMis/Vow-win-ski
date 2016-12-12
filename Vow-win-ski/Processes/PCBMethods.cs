﻿using System;

namespace Vow_win_ski.Processes
{

    public partial class PCB
    {

        /// <summary>
        /// Zamyka proces (stan = terminated)
        /// </summary>
        /// <param name="ClosingProcess">Proces zamykający (tylko, jesli przyczyna = KilledByOther, w p.p. null)</param>
        /// <returns>
        /// 0 - zamknięto proces
        /// 1 - proces oczekuje na zamkniecie
        /// 2 - jeśli zamykany przez inny proces, nie podano procesu zamykającego
        /// </returns>
        public int TerminateProcess(ReasonOfProcessTerminating Reason, PCB ClosingProcess = null, int ExitCode = 0)
        {
            throw new NotImplementedException();

            if (Reason == ReasonOfProcessTerminating.KilledByOther && ClosingProcess == null)
            {
                Console.WriteLine("Blad zamykania procesu: nie podano procesu zamykajacego.");
                return 2;
            }

            string ReasonString = "(brak powodu)";

            switch (Reason)
            {
                case ReasonOfProcessTerminating.Ended:
                    ReasonString = "Proces sie zakonczyl.";
                    break;

                case ReasonOfProcessTerminating.ThrownError:
                    ReasonString = "Wystapil blad w procesie.";
                    break;

                case ReasonOfProcessTerminating.UserClosed:
                    ReasonString = "Proces zostal zakmniety przez uzytkownika.";
                    break;

                case ReasonOfProcessTerminating.CriticalError:
                    ReasonString = "Program spowodowal wystapienie bledu krytycznego i zostal zamkniety przez system.";
                    break;

                case ReasonOfProcessTerminating.KilledByOther:
                    ReasonString = "Proces zostal zamkniety przez proces " + ClosingProcess.ToString() + ".";
                    break;

                case ReasonOfProcessTerminating.ClosingSystem:
                    ReasonString = "Proces zostal zamkniety z powodu zamykania systemu.";
                    break;
            }


            if (State == ProcessState.Running)
            {
                State = ProcessState.Terminated;
                client.Disconnect();
                CPU.Scheduler.GetInstance.RemoveProcess(this);

                Console.WriteLine("Zakonczono proces " + this.ToString() + ".");
                Console.WriteLine("Powod zamkniecia: " + ReasonString);
                this.RemoveProcess();
                return 0;

            }
            else
            {
                WaitingForStopping = true;
                Console.WriteLine("Oczekiwanie na zamkniecie procesu: " + this.ToString() + ".");
                Console.WriteLine("Proces zostanie zamkniety po przejsciu do stanu Running.");
                Console.WriteLine("Powod zamkniecia: " + ReasonString);

                //Zablokuj proces zamykający

                return 1;
            }

        }

        /// <summary>
        /// Uruchamia proces po utworzeniu go metodą CreateProcess (przejście New -> Ready)
        /// </summary>
        /// <remarks>Uruchomienie procesu - XY</remarks>  
        /// <returns>
        /// 0 - uruchomiono proces
        /// 2 - błąd: proces ma stan inny niż New
        /// </returns>
        public int RunNewProcess()
        {

            if (State == ProcessState.New)
            {
                State = ProcessState.Ready;
                Console.WriteLine("Uruchomiono proces " + this.ToString() + ".");

                CPU.Scheduler.GetInstance.AddProcess(this);
                return 0;

            }
            else
            {
                Console.WriteLine("Blad uruchamiania procesu: Proces musi miec stan New. [" + this.ToString() + "]");
                return 2;
            }

        }

        /// <summary>Uruchamia oczekujący proces. Jeśli proces miał zostać zamknięty (WaitingForStopping == True), proces jest zamykany i funkcja zwraca 2</summary>
        /// <remarks>Ready -> Running</remarks>
        /// <returns>
        /// 0 - uruchomiono     1 - stan inny niż Ready     2 - proces został zakończony
        /// </returns>
        public int RunReadyProcess()
        {
            throw new NotImplementedException();

            if (State == ProcessState.Ready)
            {

                if (WaitingForStopping)
                {
                    //odblokuj proces zamykający

                    Console.WriteLine("Odblokowano proces oczekujacy na zamkniecie biezacego procesu: ");

                    State = ProcessState.Terminated;
                    CPU.Scheduler.GetInstance.RemoveProcess(this);
                    Console.WriteLine("Zamknieto czekajacy na zamkniecie proces wchodzacy do stanu Running: " + this.ToString() + ".");

                    this.RemoveProcess();
                    return 2;

                }
                else
                {
                    State = ProcessState.Running;
                    Console.WriteLine("Uruchomiono proces czekajacy na procesor: " + this.ToString() + ".");

                    client.Connect();

                    if (ReceiverMessageLock == 1)
                    {
                        Receive();
                    }

                    return 0;
                }

            }
            else
            {
                Console.WriteLine("Blad uruchamiania czekajacego procesu: Proces ma stan inny niz Ready: " + this.ToString() + ".");
                return 1;
            }
        }

        /// <remarks>Running -> Waiting</remarks>
        /// <returns>0 - proces przeszedł w stan oczekiwania, 1 - proces ma stan inny niż Running</returns>
        public int WaitForSomething()
        {

            if (State == ProcessState.Running)
            {
                State = ProcessState.Waiting;
                client.Disconnect();
                CPU.Scheduler.GetInstance.RemoveProcess(this);

                Console.WriteLine("Proces " + this.ToString() + " przeszedl w stan oczekiwania na odblokowanie.");
                return 0;

            }
            else
            {
                Console.WriteLine("Nie udalo sie wstrzymac procesu. Proces ma stan inny niz Running: " + this.ToString() + ".");
                return 1;
            }
        }

        /// <remarks>Waiting -> Ready</remarks>
        /// <returns>0 - proces przeszedł na Ready, 1 - stan inny niż Waiting</returns>
        public int StopWaiting()
        {

            if (State == ProcessState.Waiting)
            {
                State = ProcessState.Ready;
                CPU.Scheduler.GetInstance.AddProcess(this);

                Console.WriteLine("Proces " + this.ToString() + " przeszedl w stan oczekiwania na przydzial procesora.");
                return 0;
            }
            else
            {

                Console.WriteLine("Nie udalo sie odblokowac procesu. Proces ma stan inny niz Waiting: " + this.ToString() + ".");
                return 1;
            }
        }

        /// <remarks>Running -> Ready</remarks>
        /// <returns>0 - proces przeszedł na Ready, 1 - proces ma stan inny niż Running</returns>
        public int WaitForScheduling()
        {
            if (State == ProcessState.Running)
            {
                State = ProcessState.Ready;
                client.Disconnect();

                Console.WriteLine("Przerwano realizacje przez procesor procesu: " + this.ToString() + ".");
                return 0;

            }
            else
            {

                Console.WriteLine("Blad przerywania procesu: Proces ma stan inny niz Running: " + this.ToString() + ".");
                return 1;
            }
        }

        /// <summary>Usuwa proces, który musi najpierw zostać zatrzymany przez TerminateProcess</summary>
        /// <returns>
        /// 0 - usunięto proces
        /// 2 - błąd: przed usunięciem procesu należy go zatrzymać
        /// </returns>
        /// <remarks>Usunięcie procesu - XD</remarks>
        public int RemoveProcess()
        {

            if (this.State == ProcessState.Terminated)
            {
                MemoryModule.Memory.GetInstance.RemoveFromMemory(this);
                _CreatedPCBs.Remove(this);

                Console.WriteLine("Usunieto proces " + this.ToString() + ".");
                return 0;

            }
            else
            {
                Console.WriteLine("Blad usuwania procesu: Proces nie zostal zatrzymany przed usunieciem: " + this.ToString() + ".");
                return 2;
            }

        }

        public override string ToString()
        {
            return "[" + PID.ToString() + "] " + Name + ", stan=" + State.ToString() + ", priorytet=" + CurrentPriority.ToString();
        }

        public void PrintAllFields()
        {
            Console.WriteLine("Zawartosc bloku PCB procesu:");
            Console.WriteLine("PID: " + PID.ToString());
            Console.WriteLine("Nazwa: " + Name);
            Console.WriteLine("Priorytet: " + CurrentPriority.ToString());
            Console.WriteLine("Poczatkowy priorytet: " + StartPriority.ToString());
            Console.WriteLine("Czas posiadania obecnego priorytetu: " + PriorityTime.ToString());
            Console.WriteLine("Rejestry: " + Registers.ToString());
            Console.WriteLine("Stan: " + State.ToString());
            Console.WriteLine("Licznik instrukcji: " + InstructionCounter.ToString());
            Console.WriteLine("Strony pamieci: " + MemoryBlocks.ToString());
            Console.WriteLine("Zamek odbioru wiadomosci: " + ReceiverMessageLock.ToString());
            Console.WriteLine("Oczekiwanie na zamkniecie: " + WaitingForStopping.ToString());
            Console.WriteLine("Klient odbioru wiadomosci: " + client.ToString());
            Console.WriteLine();
        }

        public void Send(string receivername, string message)
        {
            throw new NotImplementedException();

            //byte id = receiver.PID;
            //if(receiver.Lock == 0) {
            //        client._send(id, message);
            //} else {
            //    Unlock(receiver);
            //    cliend._send(id, message);
            //}
        }

        void Receive()
        {
            throw new NotImplementedException();

            if (client._receive() == false)
            {
                //Lock(this);
            }
        }

        public static bool operator ==(PCB a, PCB b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            return a.Name == b.Name;
        }

        public static bool operator !=(PCB a, PCB b)
        {
            return !(a == b);
        }

        public override bool Equals(object other)
        {
            return this == (PCB)other;
        }

    }

}
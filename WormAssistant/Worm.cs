using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace WormAssistant
{
    public class Worm : Form
    {
        private readonly float startSlideForce = 20;
        private readonly int startFallHeight = 350;
        private Rectangle currentSurface;
        private Timer decisions, painter, dragPrepareTimer;
        private bool islandSurfacesChanged, isBeingDragged;
        private Point dragLocation;
        private Action currentAction;        
        private DragMomentum dragMomentum;
        private string command;
        private PointF location;

        public Worm()
        {
            this.SuspendLayout();
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.Text = "WormAssistant";
            this.TopMost = true;
            this.TransparencyKey = Color.White;
            this.DoubleBuffered = true;
            this.Shown += this.Worm_Shown;
            this.Paint += this.Worm_Paint;
            this.MouseDown += this.Worm_MouseDown;
            this.MouseUp += this.Worm_MouseUp;
            this.ResumeLayout();
        }

        protected override void Dispose(bool disposing)
        {
            this.dragPrepareTimer.Dispose();
            this.decisions.Dispose();
            this.painter.Dispose();
            base.Dispose(disposing);
        }


        private new PointF Location
        {
            get
            {
                return new PointF(this.location.X + 30, this.location.Y + 43);
            }
            set
            {
                this.location = new PointF(value.X - 30, value.Y - 43);
                this.DesktopLocation = new Point((int)value.X - 30, (int)value.Y - 43);
            }
        }

        private Action CurrentAction
        {
            get
            {
                return this.currentAction;
            }
            set 
            {
                if (this.currentAction != null)
                {
                    this.currentAction.Dispose();
                }

                this.currentAction = value;
            }
        }

        private bool IsReadyForCommand => this.CurrentAction.Type == Action.Types.Walk || this.CurrentAction.Type == Action.Types.Stay || this.CurrentAction.Type == Action.Types.BreathHard;

        private bool IsBeingDragged
        {
            get
            {
                return this.isBeingDragged;
            }
            set
            {
                if (value)
                {
                    this.isBeingDragged = true;
                    this.dragMomentum = new DragMomentum(this.Location);
                }
                else
                {
                    this.isBeingDragged = false;
                    this.dragMomentum = null;
                }
            }
        }

        public void PerformAction()
        {
            if (this.IsBeingDragged)
            {
                this.dragMomentum.AddLocation(this.Location);
            }
            else if (this.CurrentAction.IsDynamic)
            {
                this.ProcessDynamicAction();
            }
            else if (this.CurrentAction.Animation.IsFinished)
            {
                this.SwitchToActionNextStage();
            }

            if (this.CurrentAction.Force.Y >= 0 && this.islandSurfacesChanged)
            {
                this.ProcessSurfacesChanges();
            }
        }

        private void StartNewAction(bool isWormWeak)
        {
            var rand = new Random();
            if (isWormWeak || rand.Next(3) != 2)
            {
                this.StartRandomActionWithDecisions(isWormWeak, rand);
            }
            else
            {
                this.StartRandomActionWithoutDecisions(rand);
            }
        }

        private void StartRandomActionWithDecisions(bool isWormWeak, Random rand)
        {
            this.decisions.Interval = rand.Next(2) == 0 ? rand.Next(1, 3) * 10000 : rand.Next(3, 6) * 1000;
            var animationSpeed = rand.Next(20, 80);
            if (isWormWeak)
            {
                this.CurrentAction = new Action(Action.Types.Stay, new Animation(new List<Animation.FramesSet>
                {
                    new Animation.FramesSet { Pic = Properties.Resources.breathhard, IsReverced = false },
                    new Animation.FramesSet { Pic = Properties.Resources.breathhard, IsReverced = true }
                }, this.CurrentAction.Animation.Orientation, animationSpeed, true));
            }
            else
            {
                switch (rand.Next(3))
                {
                    case 0:
                    case 1:
                        Directions direction = (Directions)rand.Next(2);
                        if (this.CurrentAction.Type == Action.Types.Walk && this.CurrentAction.Animation.Orientation == direction)
                        {
                            this.CurrentAction.Force = new Vector((float)Space.TimeSpeed / animationSpeed * (this.CurrentAction.Force.X > 0 ? 1 : -1), 0);
                            this.CurrentAction.Animation.Speed = animationSpeed;
                        }
                        else
                        {
                            this.CurrentAction = new Action(Action.Types.Walk,
                                new Vector((float)Space.TimeSpeed / animationSpeed * (direction == Directions.Right ? 1 : -1), 0),
                                new Animation(new List<Animation.FramesSet>
                                {
                                    new Animation.FramesSet { Pic = Properties.Resources.walk },
                                }, direction, animationSpeed, true));
                        }
                        break;
                    default:
                        if (this.CurrentAction.Type == Action.Types.Stay)
                        {
                            this.CurrentAction.Animation.Speed = animationSpeed;
                        }
                        else
                        {
                            this.CurrentAction = new Action(Action.Types.Stay, new Animation(new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.breath, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.breath, IsReverced = true }
                            }, this.CurrentAction.Animation.Orientation, animationSpeed, true));
                        }
                        break;
                }
            }

            this.decisions.Start();
        }

        private void StartRandomActionWithoutDecisions(Random rand)
        {
            var animationSpeed = rand.Next(30, 101);
            Action.Types actionType;
            List<Animation.FramesSet> animationFrameset;
            switch (rand.Next(4))
            {
                case 0:
                case 2:
                    actionType = Action.Types.Blink;
                    animationFrameset = new List<Animation.FramesSet>
                    {
                        new Animation.FramesSet { Pic = Properties.Resources.blink, IsReverced = false },
                        new Animation.FramesSet { Pic = Properties.Resources.blink, IsReverced = true }
                    };
                    break;
                case 1:
                    switch (rand.Next(19))
                    {
                        case 0:
                            actionType = Action.Types.Angry;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.angry, IsReverced = false }
                            };
                            break;
                        case 1:
                            actionType = Action.Types.BigHead;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.bighead, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.bighead, IsReverced = true }
                            };
                            break;
                        case 2:
                            actionType = Action.Types.BlinkHard;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.blinkhard, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.blinkhard, IsReverced = true }
                            };
                            break;
                        case 3:
                            actionType = Action.Types.Eat;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.eat }
                            };
                            break;
                        case 4:
                            actionType = Action.Types.LookPeek;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.lookpeek, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.lookpeek, IsReverced = true }
                            };
                            break;
                        case 5:
                            actionType = Action.Types.LookScreen;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.lookscreen, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.lookscreen, IsReverced = true }
                            };
                            break;
                        case 6:
                            actionType = Action.Types.LookSideways;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.looksidewaysin },
                                new Animation.FramesSet { Pic = Properties.Resources.looksideways },
                                new Animation.FramesSet { Pic = Properties.Resources.looksidewaysout }
                            };
                            break;
                        case 7:
                            actionType = Action.Types.LookUp;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.lookup, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.lookup, IsReverced = true }
                            };
                            break;
                        case 8:
                            actionType = Action.Types.LookUpBlink;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.lookupblink, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.lookupblink, IsReverced = true }
                            };
                            break;
                        case 9:
                            actionType = Action.Types.LookUpHead;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.lookuphead, IsReverced = false},
                                new Animation.FramesSet { Pic = Properties.Resources.lookuphead, IsReverced = true }
                            };
                            break;
                        case 10:
                            actionType = Action.Types.Mustache;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.mustache, IsReverced = false }
                            };
                            break;
                        case 11:
                            actionType = Action.Types.PeekBlink;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.peekblink, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.peekblink, IsReverced = true }
                            };
                            break;
                        case 12:
                            actionType = Action.Types.PeekHeadBlink;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.peekheadblink, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.peekheadblink, IsReverced = true }
                            };
                            break;
                        case 13:
                            actionType = Action.Types.Sad;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.sadin, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.sad, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.sadin, IsReverced = true }
                            };
                            break;
                        case 14:
                            actionType = Action.Types.SadSneeze;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.sadin, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.sad, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.sadsneeze, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.sad, IsReverced = false },
                                new Animation.FramesSet { Pic = Properties.Resources.sadin, IsReverced = true }
                            };
                            break;
                        case 15:
                            actionType = Action.Types.Scratch;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.scratch }
                            };
                            break;
                        case 16:
                            actionType = Action.Types.Silly;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.silly }
                            };
                            break;
                        case 17:
                            actionType = Action.Types.SkipIn;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.skipin }
                            };
                            break;
                        default:
                            actionType = Action.Types.FlagIn;
                            animationFrameset = new List<Animation.FramesSet>
                            {
                                new Animation.FramesSet { Pic = Properties.Resources.flagin }
                            };
                            break;
                    }
                    break;
                default:
                    actionType = Action.Types.Jump;
                    animationFrameset = new List<Animation.FramesSet>
                    {
                        new Animation.FramesSet { Pic = Properties.Resources.jump}
                    };
                    break;
            }

            this.CurrentAction = new Action(actionType, new Animation(animationFrameset, this.CurrentAction.Animation.Orientation, animationSpeed, false));
        }

        private void ProcessDynamicAction()
        {
            var nextLocation = new PointF(this.Location.X + this.CurrentAction.Force.X, this.Location.Y + this.CurrentAction.Force.Y);
            var nextForce = this.CurrentAction.Force + this.CurrentAction.Velocity;

            if (this.CurrentAction.Velocity.X != 0 && Math.Sign(this.CurrentAction.Force.X) != Math.Sign(nextForce.X) ||
                nextForce.Y > 0 && nextLocation.Y >= this.CurrentAction.YEndPoint)
            {
                this.Location = new PointF(this.Location.X, this.CurrentAction.YEndPoint);
                this.SwitchToActionNextStage();
            }
            else
            {
                this.Location = new PointF(nextLocation.X, nextLocation.Y);
                this.CurrentAction.Force = nextForce;
                this.ProcessSurfaceEdge();
            }
        }

        private void SwitchToActionNextStage()
        {
            var rand = new Random();
            switch (this.CurrentAction.Type)
            {
                case Action.Types.Jump:
                    var force = new Vector(rand.Next(1, 11), -this.CurrentAction.Animation.Speed * 0.5f);
                    if (this.CurrentAction.Animation.Orientation == Directions.Left)
                    {
                        force = new Vector(-force.X, force.Y);
                    }

                    this.Push(force);
                    break;
                case Action.Types.Hang:
                    this.Push(this.CurrentAction.Force);
                    break;
                case Action.Types.GoingUp:
                    this.currentSurface = GetNewMoveBounds();
                    var actionYEndPoint = this.currentSurface.Top;
                    var actionType = Action.Types.GoingDown;
                    if (this.currentSurface.Y - this.Location.Y >= this.startFallHeight)
                    {
                        switch (rand.Next(2))
                        {
                            case 0:
                                actionYEndPoint = (int)this.Location.Y + rand.Next(100, 200);
                                actionType = Action.Types.BeginFall;
                                break;
                            case 1:
                                actionType = Action.Types.GoingDownHard;
                                break;
                        }
                    }

                    this.CurrentAction = new Action(actionType, this.CurrentAction.Force,
                        this.CurrentAction.Velocity, actionYEndPoint,
                        new Animation(new List<Animation.FramesSet>
                        {
                            new Animation.FramesSet { Pic = Properties.Resources.movedown }
                        }, this.CurrentAction.Animation.Orientation, 40, true));
                    break;
                case Action.Types.GoingDown:
                    if (Math.Abs(CurrentAction.Force.X) >= this.startSlideForce)
                    {
                        this.StartSlideAction();
                    }
                    else
                    {
                        this.CurrentAction = new Action(Action.Types.Land,
                             new Animation(new List<Animation.FramesSet>
                        {
                            new Animation.FramesSet { Pic = Properties.Resources.land }
                        }, this.CurrentAction.Animation.Orientation, 60, false));
                    }

                    break;
                case Action.Types.GoingDownHard:
                    if (Math.Abs(this.CurrentAction.Force.X) >= this.startSlideForce)
                    {
                        StartSlideAction();
                    }
                    else
                    {
                        this.CurrentAction = new Action(Action.Types.Land,
                            new Animation(new List<Animation.FramesSet>
                        {
                            new Animation.FramesSet { Pic = Properties.Resources.landhard }
                        }, CurrentAction.Animation.Orientation, 70, false));
                    }

                    break;
                case Action.Types.BeginFall:
                    actionType = Action.Types.FallRoll;
                    var animationIsLooped = true;
                    var animationResource = Properties.Resources.fallroll;
                    if (rand.Next(2) == 1)
                    {
                        actionType = Action.Types.FallStraight;
                        animationIsLooped = false;
                        animationResource = Properties.Resources.fall;
                    }

                    this.CurrentAction = new Action(actionType, this.CurrentAction.Force,
                        this.CurrentAction.Velocity, this.currentSurface.Top,
                        new Animation(new List<Animation.FramesSet>
                        {
                            new Animation.FramesSet { Pic = animationResource }
                        }, this.CurrentAction.Animation.Orientation, 1, animationIsLooped));
                    break;
                case Action.Types.FallRoll:
                    if (rand.Next(2) == 0 || Math.Abs(this.CurrentAction.Force.X) >= this.startSlideForce)
                    {
                        this.StartSlideAction();
                    }
                    else
                    {
                        this.CurrentAction = new Action(Action.Types.Stuck,
                            new Animation(new List<Animation.FramesSet>
                        {
                            new Animation.FramesSet { Pic = Properties.Resources.stuck }
                        }, this.CurrentAction.Animation.Orientation, 40, false));
                    }

                    break;
                case Action.Types.Slide:
                    this.CurrentAction = new Action(Action.Types.SlideOut,
                        new Animation(new List<Animation.FramesSet>
                        {
                            new Animation.FramesSet { Pic = Properties.Resources.slideout }
                        }, this.CurrentAction.Animation.Orientation, 40, false));
                    break;
                case Action.Types.FallStraight:
                    if (Math.Abs(this.CurrentAction.Force.X) >= this.startSlideForce)
                    {
                        this.StartSlideAction();
                    }
                    else
                    {
                        this.CurrentAction = new Action(Action.Types.Stuck,
                            new Animation(new List<Animation.FramesSet>
                        {
                            new Animation.FramesSet { Pic = Properties.Resources.stuck }
                        }, this.CurrentAction.Animation.Orientation, 40, false));
                    }

                    break;
                case Action.Types.Stuck:
                    this.StartNewAction(true);
                    break;
                case Action.Types.SlideOut:
                    this.StartNewAction(rand.Next(2) == 0);
                    break;
                case Action.Types.LookFromEdge:
                    StartNewAction(false);
                    break;
                case Action.Types.ListenIn:
                    this.KeyPress += Worm_KeyPress;
                    this.CurrentAction = new Action(Action.Types.Listen, new Animation(new List<Animation.FramesSet>
                    {
                        new Animation.FramesSet { Pic = Properties.Resources.listen, IsReverced = false },
                        new Animation.FramesSet { Pic = Properties.Resources.listen, IsReverced = true }
                    }, this.CurrentAction.Animation.Orientation, 300, true));
                    decisions.Interval = 5000;
                    decisions.Start();
                    break;
                case Action.Types.Listen:
                case Action.Types.CommandExecute:
                    this.command = null;
                    this.KeyPress -= Worm_KeyPress;
                    this.CurrentAction = new Action(Action.Types.ListenOut,
                        new Animation(new List<Animation.FramesSet>
                    {
                        new Animation.FramesSet { Pic = Properties.Resources.listenout }
                    }, this.CurrentAction.Animation.Orientation, 40, false));
                    break;
                case Action.Types.SkipIn:
                    this.CurrentAction = new Action(Action.Types.Skip, new Animation(new List<Animation.FramesSet>
                    {
                        new Animation.FramesSet { Pic = Properties.Resources.skip, IsReverced = false }
                    }, this.CurrentAction.Animation.Orientation, rand.Next(35, 50), true));
                    this.decisions.Interval = rand.Next(3, 6) * 1000;
                    this.decisions.Start();
                    break;
                case Action.Types.FlagIn:
                    this.CurrentAction = new Action(Action.Types.Flag, new Animation(new List<Animation.FramesSet>
                    {
                        new Animation.FramesSet { Pic = Properties.Resources.flag, IsReverced = false }
                    }, this.CurrentAction.Animation.Orientation, rand.Next(35, 50), true));
                    this.decisions.Interval = rand.Next(3, 6) * 1000;
                    this.decisions.Start();
                    break;
                case Action.Types.Skip:
                    this.CurrentAction = new Action(Action.Types.SkipOut, new Animation(new List<Animation.FramesSet>
                    {
                        new Animation.FramesSet { Pic = Properties.Resources.skipout, IsReverced = false }
                    }, this.CurrentAction.Animation.Orientation, rand.Next(30, 101), false));
                    break;
                case Action.Types.Flag:
                    this.CurrentAction = new Action(Action.Types.FlagOut, new Animation(new List<Animation.FramesSet>
                    {
                        new Animation.FramesSet { Pic = Properties.Resources.flagout, IsReverced = false }
                    }, this.CurrentAction.Animation.Orientation, rand.Next(30, 101), false));
                    break;
                case Action.Types.Die:
                    this.Close();
                    break;
                default:
                    this.StartNewAction(false);
                    break;
            }
        }

        private void ProcessSurfaceEdge()
        {
            if ((this.Location.X <= this.currentSurface.Left || this.Location.X <= Space.Surface.Left) && this.CurrentAction.Force.X < 0 ||
                (this.Location.X >= this.currentSurface.Right || this.Location.X >= Space.Surface.Right) && this.CurrentAction.Force.X > 0)
            {
                if (!this.IsSurfaceEdgeActionPerformed())
                {
                    this.ChangeActionDirection();
                }
            }
        }

        private bool IsSurfaceEdgeActionPerformed()
        {
            if (this.Location.X < Space.Surface.Right && this.Location.X > Space.Surface.Left)
            {
                switch (this.CurrentAction.Type)
                {
                    case Action.Types.Walk:
                        switch (new Random().Next(5))
                        {
                            case 0:
                                this.CurrentAction = new Action(Action.Types.LookFromEdge,
                                    new Animation(new List<Animation.FramesSet>
                                    {
                                        new Animation.FramesSet { Pic = Properties.Resources.looksidewaysin, IsReverced = false },
                                        new Animation.FramesSet { Pic = Properties.Resources.looksideways, IsReverced = false },
                                        new Animation.FramesSet { Pic = Properties.Resources.looksidewaysout, IsReverced = false }
                                    }, this.CurrentAction.Animation.Orientation, 80, false));
                                return true;
                            case 2:
                                this.FallFromEdge();
                                return true;
                        }

                        break;
                    case Action.Types.Slide:
                        this.FallFromEdge();
                        return true;
                    case Action.Types.GoingUp:
                    case Action.Types.GoingDown:
                    case Action.Types.BeginFall:
                    case Action.Types.FallRoll:
                    case Action.Types.FallStraight:
                    case Action.Types.GoingDownHard:
                        return true;
                }
            }

            return false;
        }

        private void FallFromEdge()
        {
            this.islandSurfacesChanged = true;
            this.Location = new PointF(this.Location.X + this.CurrentAction.Force.X, this.Location.Y + this.CurrentAction.Force.Y);
        }

        private void ChangeActionDirection()
        {
            var direction = Directions.Left;
            if (this.CurrentAction.Direction == Directions.Left)
            {
                direction = Directions.Right;
            }

            this.CurrentAction.ChangeDirection(direction);
            this.CurrentAction.Animation.ChangeOrientation(direction);
        }

        private void ProcessSurfacesChanges()
        {
            if (this.CurrentAction.Type != Action.Types.GoingUp && CurrentAction.Type != Action.Types.BeginFall)
            {
                if (this.Location.Y != Space.Surface.Top)
                {
                    var avaiableSurfaces = ActiveWindowsSurfaceMonitor.GetActiveSurfaces();
                    var standSurface = avaiableSurfaces.FirstOrDefault(s => s.Top == this.Location.Y && this.Location.X >= s.Left && this.Location.X <= s.Right);
                    if (standSurface.IsEmpty)
                    {
                        if (this.CurrentAction.Force.Y == 0)
                        {
                            this.Push(this.CurrentAction.Force);
                            this.islandSurfacesChanged = false;
                        }
                        else if (this.CurrentAction.Force.Y > 0)
                        {
                            var surfaceforLanding = avaiableSurfaces.FirstOrDefault(s => this.Location.Y < s.Top /*&& this.Location.Y + this.CurrentAction.Force.Y > s.Top*/ && s.Left <= this.Location.X && s.Right >= this.Location.X);
                            this.CurrentAction.YEndPoint = surfaceforLanding.IsEmpty ? Space.Surface.Top : surfaceforLanding.Top;
                            return;
                        }
                    }
                    else
                    {
                        this.currentSurface = standSurface;
                    }
                }
            }

            this.islandSurfacesChanged = false;
        }

        private Rectangle GetNewMoveBounds()
        {
            var virtualCoords = new List<PointF>();
            var virtualLocation = Location;
            var virtualForce = this.CurrentAction.Force;
            var virtualVelocity = this.CurrentAction.Velocity;
            while (virtualLocation.Y < Space.Surface.Top)
            {
                virtualCoords.Add(new PointF(virtualLocation.X, virtualLocation.Y));
                virtualLocation = new PointF(virtualLocation.X + virtualForce.X, virtualLocation.Y + virtualForce.Y);
                virtualForce += virtualVelocity;
                if (virtualLocation.X <= Space.Surface.Left && virtualForce.X < 0 || virtualLocation.X >= Space.Surface.Right && virtualForce.X > 0)
                {
                    virtualForce = new Vector(-virtualForce.X, virtualForce.Y);
                    virtualVelocity = new Vector(-virtualVelocity.X, virtualVelocity.Y);
                }
            }

            return this.LookupSurfaceToLandOn(ActiveWindowsSurfaceMonitor.GetActiveSurfaces(), virtualCoords);
        }

        private Rectangle LookupSurfaceToLandOn(Rectangle[] surfaces, List<PointF> virtualCoords)
        {
            if (surfaces != null)
            {
                foreach (var surface in surfaces)
                {
                    var lastCoordinateBeforeLanding = virtualCoords.LastOrDefault(p => p.Y <= surface.Y);
                    if (!lastCoordinateBeforeLanding.IsEmpty && lastCoordinateBeforeLanding.X >= surface.Left && lastCoordinateBeforeLanding.X <= surface.Right)
                    {
                        return surface;
                    }
                }
            }

            return Space.Surface;
        }

        private void Push(Vector force)
        {
            this.decisions.Stop();
            var velocity = new Vector(0, Space.Gravity);
            this.CurrentAction = new Action(Action.Types.GoingUp, force, velocity, Action.CalculateYEndPoint(Location.Y, force.Y, velocity.Y),
                new Animation(new List<Animation.FramesSet>
                {
                    new Animation.FramesSet { Pic = Properties.Resources.moveup }
                }, this.CurrentAction.Animation.Orientation, 40, true));
        }

        private void StartSlideAction()
        {
            this.CurrentAction = new Action(Action.Types.Slide, new Vector(this.CurrentAction.Force.X / 5, 0),
                new Vector(this.CurrentAction.Force.X > 0 ? -0.1f : 0.1f, 0), this.CurrentAction.YEndPoint,
                new Animation(new List<Animation.FramesSet>
                {
                    new Animation.FramesSet { Pic = Properties.Resources.slide }
                }, this.CurrentAction.Animation.Orientation, 40, true));
        }

        private void Worm_Shown(object sender, EventArgs e)
        {
            this.islandSurfacesChanged = false;
            this.Size = new Size(64, 64);
            ActiveWindowsSurfaceMonitor.DesktopWindowsChanged += ActiveWindowsSurfaceMonitor_WindowMoved;
            var rand = new Random();
            this.Location = new Point(rand.Next(Screen.PrimaryScreen.WorkingArea.Width), rand.Next(Screen.PrimaryScreen.WorkingArea.Height));
            this.CurrentAction = new Action(Action.Types.Hang,
                new Animation(new List<Animation.FramesSet>
                {
                    new Animation.FramesSet { Pic = Properties.Resources.fade, IsReverced = true }
                }, (Directions)new Random().Next(2), 40, false));
            this.painter = new Timer { Interval = 1 };
            this.painter.Tick += new EventHandler(Painter_Tick);
            this.decisions = new Timer();
            this.decisions.Tick += new EventHandler(Decisions_Tick);
            this.painter.Start();
            this.dragPrepareTimer = new Timer { Interval = 100 };
        }

        private void ActiveWindowsSurfaceMonitor_WindowMoved(object sender, EventArgs e)
        {
            this.islandSurfacesChanged = true;
        }

        private void Painter_Tick(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void Worm_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImageUnscaled(this.CurrentAction.Animation.CurrentFrame, 0, 0);
        }

        private void Decisions_Tick(object sender, EventArgs e)
        {
            this.decisions.Stop();
            this.SwitchToActionNextStage();
        }

        private void Worm_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.dragPrepareTimer.Tick += new EventHandler(DragPrepareTimer_Tick);
                this.dragPrepareTimer.Start();
                this.dragLocation = Cursor.Position;
            }
            else this.Close();
        }

        private void DragPrepareTimer_Tick(object sender, EventArgs e)
        {
            this.dragPrepareTimer.Stop();
            if (this.CurrentAction.Type != Action.Types.Listen)
            {
                this.MouseMove += Worm_MouseMove;
                this.decisions.Stop();
                this.IsBeingDragged = true;
                this.CurrentAction = new Action(Action.Types.Hang, new Animation(
                    new List<Animation.FramesSet>
                    {
                        new Animation.FramesSet{Pic=Properties.Resources.hang,IsReverced=false},
                        new Animation.FramesSet{Pic=Properties.Resources.hang,IsReverced=true}
                    }, this.CurrentAction.Animation.Orientation, 50, true));
            }
        }

        private void Worm_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var newMousePosition = Cursor.Position;
                this.Location = new PointF(this.Location.X - (this.dragLocation.X - newMousePosition.X), this.Location.Y - (this.dragLocation.Y - newMousePosition.Y));
                this.dragLocation = newMousePosition;
            }
        }

        private void Worm_MouseUp(object sender, MouseEventArgs e)
        {
            this.dragPrepareTimer.Tick -= DragPrepareTimer_Tick;
            this.dragPrepareTimer.Stop();
            if (this.IsBeingDragged)
            {
                this.MouseMove -= Worm_MouseMove;
                this.Push(this.dragMomentum.Force);
                this.IsBeingDragged = false;
            }
            else if (e.Button == MouseButtons.Left && this.IsReadyForCommand)
            {
                this.decisions.Stop();
                this.CurrentAction = new Action(Action.Types.ListenIn,
                    new Animation(new List<Animation.FramesSet>
                    {
                        new Animation.FramesSet { Pic = Properties.Resources.listenin }
                    }, this.CurrentAction.Animation.Orientation, 40, false));
            }
        }

        void Worm_KeyPress(object sender, KeyPressEventArgs e)
        {
            this.command += e.KeyChar;
            if (string.Equals(command, "die", StringComparison.OrdinalIgnoreCase))
            {
                this.decisions.Stop();
                this.CurrentAction = new Action(Action.Types.Die, new Animation(new List<Animation.FramesSet>
                {
                    new Animation.FramesSet{ Pic = Properties.Resources.death },
                    new Animation.FramesSet{ Pic = Properties.Resources.circle25 }
                }, this.CurrentAction.Animation.Orientation, 40, false));
            }
        }
    }
}

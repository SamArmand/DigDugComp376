using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace DigDugComp376
{
	sealed class Monster : Sprite
	{
		internal readonly Fire Fire = new Fire();

		internal enum State
		{
			Walking,
			Ghost,
			Dragon,
			Growing,
			Dead
		}

		internal State CurrentState = Walking;

		const State Walking = State.Walking,
					Ghost = State.Ghost,
					Dragon = State.Dragon,
					Growing = State.Growing,
					Dead = State.Dead;

		readonly bool _isFygar;

		readonly int _monsterSpeed,
					 _y,
					 _multiplier;

		readonly Stopwatch _stopwatch = new Stopwatch(),
						   _fireWatch = new Stopwatch(),
						   _ghostWatch = new Stopwatch(),
						   _timeAsGhost = new Stopwatch(),
						   _walkWatch = new Stopwatch(),
						   _deadWatch = new Stopwatch(),
						   _game1Stopwatch = Game1.Stopwatch;

		readonly Rectangle _rectangle = new Rectangle(56, 0, 56, 56);

		readonly Vector2 _originalPosition;

		readonly Hose _hose;
		readonly DigDug _digDug;

		int _growCount;

		byte[,] _level;

		State _lastState;

		Vector2 _lastSpeed,
				_digDugLastKnownPosition;

		internal Monster(int x, int y, bool isFygar, int multiplier, int monsterSpeed) : base(isFygar ? Game1.FygarTexture : Game1.PookaTexture)
		{
			_isFygar = isFygar;
			_monsterSpeed = monsterSpeed;
			Visible = true;
			_y = y;

			_originalPosition = new Vector2(x * 56, y * 56);
			Position = _originalPosition;
			Source = _rectangle;

			_digDug = Game1.DigDug;
			_hose = _digDug.Hose;
			_digDugLastKnownPosition = _digDug.Position;

			_multiplier = multiplier;
		}

		internal void Update()
		{
			if (!Visible) return;

			var (x, y) = Position;
			_level = Game1.Level;

			var fireWatchElapsed = _fireWatch.ElapsedMilliseconds;

			switch (CurrentState)
			{
				case Dead when _deadWatch.ElapsedMilliseconds >= 1000:
					Visible = false;
					_deadWatch.Stop();
					break;
				case Walking when fireWatchElapsed >= RandomNext(10000, 20000) && _isFygar:
					Fire.Visible = true;
					_walkWatch.Reset();
					CurrentState = Dragon;
					Fire.Position = new Vector2(x + (Flip ? 56 : -56), y);
					_fireWatch.Restart();
					break;
				case Walking when _ghostWatch.ElapsedMilliseconds >= RandomNext(10000, 20000) && _game1Stopwatch.ElapsedMilliseconds >= 5000:
					_walkWatch.Reset();
					Source.X = 0;
					Source.Y = 0;
					CurrentState = Ghost;
					_ghostWatch.Reset();
					_timeAsGhost.Start();
					break;
				case Ghost when !(Math.Abs(x % 56) > 0 || Math.Abs(y % 56) > 0) && _level[(int) x / 56, (int) y / 56] == 0 && _timeAsGhost.ElapsedMilliseconds >= 1000:
					_timeAsGhost.Reset();
					Source.X = 56;
					CurrentState = Walking;
					_walkWatch.Start();
					_ghostWatch.Start();
					break;
				case Dragon when fireWatchElapsed >= 2000:
					Fire.Visible = false;
					_fireWatch.Restart();
					_walkWatch.Start();
					CurrentState = Walking;
					break;
				case Walking when Collides(_hose):
					_walkWatch.Reset();
					_lastState = CurrentState;
					Source.Y = 0;
					CurrentState = Growing;
					_stopwatch.Start();
					break;
				case Growing when _stopwatch.ElapsedMilliseconds >= 1000:
					if (Collides(_hose)) ++_growCount;
					else if (_growCount > 0) --_growCount;

					switch (_growCount)
					{
						case 0:
							CurrentState = _lastState;
							_stopwatch.Stop();
							break;
						case 4:
							Die(1);
							break;
					}

					Source.X = CurrentState == Ghost ? 0 : 56 * (_growCount + 1);

					_stopwatch.Restart();
					break;
			}

			UpdateMovement(x, y);
		}

		internal void Die(int method)
		{
			_deadWatch.Start();
			++Game1.DeadMonsters;
			CurrentState = Dead;
			Game1.Score += _multiplier * method * (_y + 1) * 10;
		}

		internal void Reset()
		{
			if (CurrentState == Dead) return;

			Position = _originalPosition;
			CurrentState = Walking;
			Source = _rectangle;
			Flip = false;
			Fire.Flip = false;
			Fire.Visible = false;
			_stopwatch.Reset();
			_fireWatch.Reset();
			_ghostWatch.Reset();
			_timeAsGhost.Reset();
			_walkWatch.Reset();
			_deadWatch.Reset();
		}

		internal void Play()
		{
			_fireWatch.Start();
			_ghostWatch.Start();
			_walkWatch.Start();
		}

		void UpdateMovement(float x, float y)
		{
			if (CurrentState != Ghost)
			{
				var dp = _digDug.Position;

				if (Math.Abs(dp.X % 56) <= 0 && Math.Abs(dp.Y % 56) <= 0) _digDugLastKnownPosition = dp;
			}

			switch (CurrentState)
			{
				case Ghost:
					var (dx, dy) = _digDugLastKnownPosition;

					var monsterSpeed2 = _monsterSpeed / 2f;

					if (dx > x)
					{
						Position.X += monsterSpeed2;
						Flip = true;
						Fire.Flip = true;
					}
					else if (dx < x)
					{
						Position.X -= monsterSpeed2;
						Flip = false;
						Fire.Flip = false;
					}
					if (dy > y) Position.Y += monsterSpeed2;
					else if (dy < y) Position.Y -= monsterSpeed2;
					break;
				case Walking:
					if (Math.Abs(x % 56) > 0 || Math.Abs(y % 56) > 0) Position += _lastSpeed;

					else
					{
						var choose = false;

						var x1 = (int) (x / 56);
						var y1 = (int) (y / 56);

						while (!choose)
						{
							switch (RandomNext(0, 4))
							{
								case 0 when Math.Abs(x) > 0 && _level[x1 - 1, y1] == 0:
									Position.X -= _monsterSpeed;

									_lastSpeed.X = -_monsterSpeed;
									_lastSpeed.Y = 0;

									choose = true;
									break;
								case 1 when Math.Abs(x - 952) > 0 && _level[x1 + 1, y1] == 0:
									Position.X += _monsterSpeed;

									_lastSpeed.X = _monsterSpeed;
									_lastSpeed.Y = 0;

									choose = true;
									break;
								case 2 when Math.Abs(y) > 0 && _level[x1, y1 - 1] == 0:
									Position.Y -= _monsterSpeed;

									_lastSpeed.X = 0;
									_lastSpeed.Y = -_monsterSpeed;

									choose = true;
									break;
								case 3 when Math.Abs(y - 728) > 0 && _level[x1, y1 + 1] == 0:
									Position.Y += _monsterSpeed;

									_lastSpeed.X = 0;
									_lastSpeed.Y = _monsterSpeed;

									choose = true;
									break;
							}

							if (_lastSpeed.X > 0)
							{
								Flip = true;
								Fire.Flip = true;
							}
							else if (_lastSpeed.X < 0)
							{
								Flip = false;
								Fire.Flip = false;
							}
						}
					}

					var elapsed = _walkWatch.ElapsedMilliseconds;

					if (elapsed >= 500)
					{
						Source.Y = 56;
						_walkWatch.Restart();
					}
					else if (elapsed >= 250) Source.Y = 0;

					break;
			}
		}
	}
}
﻿using System;
using System.Collections.Generic;
using Tp.Core.Annotations;

namespace Tp.Core
{
	public static class Try
	{
		public static Try<T> Create<T>([InstantHandle] Func<T> process)
		{
			try
			{
				return new Success<T>(process());
			}
			catch (Exception e)
			{
				return new Failure<T>(e);
			}
		}

		public static Try<T> Success<T>(T value)
		{
			return new Success<T>(value);
		}

		public static Try<T> Failure<T>(Exception e)
		{
			return new Failure<T>(e);
		}
	}

	//// ReSharper disable InconsistentNaming
	public interface Try<T>
	//// ReSharper restore InconsistentNaming
	{
		T GetOrElse(Func<T> @default);
		Try<T> OrElse(Func<Try<T>> @default);
		Maybe<T> ToMaybe();

		Try<U> Select<U>(Func<T, U> selector);
		Try<T> Where(Func<T, bool> filter);
		Try<U> SelectMany<U>(Func<T, Try<U>> selector);

		T Value { get; }
		bool IsSuccess { get; }
		void Switch([InstantHandle]Action<T> sucess, [InstantHandle]Action<Exception> exception);

		Try<T> Recover([InstantHandle]Func<Exception, T> recover);
		Try<T> Recover([InstantHandle]Func<Exception, Try<T>> recover);
	}
	public sealed class Success<T> : Try<T>, IEquatable<Success<T>>
	{
		private readonly T _value;

		public Success(T value)
		{
			_value = value;
		}

		public T GetOrElse(Func<T> @default)
		{
			return _value;
		}

		public Try<T> OrElse(Func<Try<T>> @default)
		{
			return this;
		}

		public Maybe<T> ToMaybe()
		{
			return Maybe.Just(_value);
		}

		public Try<U> Select<U>(Func<T, U> selector)
		{
			return Try.Create(() => selector(_value));
		}

		public Try<T> Where(Func<T, bool> filter)
		{
			return Try.Create<Try<T>>(() =>
			{
				if (filter(_value))
					return this;
				return new Failure<T>(new ArgumentOutOfRangeException(string.Format("Predicate does not hold for {0}", _value)));
			}
				).Flatten();
		}

		public Try<U> SelectMany<U>(Func<T, Try<U>> selector)
		{
			try
			{
				return selector(_value);
			}
			catch (Exception e)
			{
				return new Failure<U>(e);
			}
		}

		public T Value
		{
			get { return _value; }
		}

		public bool IsSuccess
		{
			get { return true; }
		}

		public void Switch(Action<T> sucess, Action<Exception> exception)
		{
			sucess(_value);
		}

		public Try<T> Recover(Func<Exception, T> recover)
		{
			return this;
		}

		public Try<T> Recover(Func<Exception, Try<T>> recover)
		{
			return this;
		}

		public bool Equals(Success<T> other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return EqualityComparer<T>.Default.Equals(_value, other._value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is Success<T> && Equals((Success<T>)obj);
		}

		public override int GetHashCode()
		{
			return EqualityComparer<T>.Default.GetHashCode(_value);
		}
	}

	public sealed class Failure<T> : Try<T>
	{
		public Exception Exception { get; private set; }

		public Failure(Exception exception)
		{
			Exception = exception;
		}

		public T GetOrElse(Func<T> @default)
		{
			return @default();
		}

		public Try<T> OrElse(Func<Try<T>> @default)
		{
			return Try.Create(@default).Flatten();
		}

		public Maybe<T> ToMaybe()
		{
			return Maybe.Nothing;
		}

		public Try<U> Select<U>(Func<T, U> selector)
		{
			return new Failure<U>(Exception);
		}

		public Try<T> Where(Func<T, bool> filter)
		{
			return this;
		}

		public Try<U> SelectMany<U>(Func<T, Try<U>> selector)
		{
			return new Failure<U>(Exception);
		}

		public T Value
		{
			get { throw Exception; }
		}

		public bool IsSuccess
		{
			get { return false; }
		}

		public void Switch(Action<T> sucess, Action<Exception> exception)
		{
			exception(Exception);
		}

		public Try<T> Recover(Func<Exception, T> recover)
		{
			return Try.Create(() => recover(Exception));
		}

		public Try<T> Recover(Func<Exception, Try<T>> recover)
		{
			return Try.Create(() => recover(Exception)).Flatten();
		}
	}
}

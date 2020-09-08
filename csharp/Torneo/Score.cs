using System;
using System.Globalization;

/// <summary>
/// Provides an abstraction of a score in a tournament.
/// </summary>
public abstract class Score : IComparable<Score>, IEquatable<Score>
{
    /// <summary>
    /// Adds one Score to another yielding their sum.
    /// </summary>
    /// <param name="score1">The first addend.</param>
    /// <param name="score2">The second addend.</param>
    /// <returns>A new Score representing the sum of the two addends.</returns>
    public static Score operator +(Score score1, Score score2)
    {
        if (score1 != null)
        {
            return score1.Add(score2);
        }

        if (score2 != null)
        {
            return score2.Add(score1);
        }

        return null;
    }

    /// <summary>
    ///  Determines whether two specified instances of Score are equal.
    /// </summary>
    /// <param name="score1">The first score for which to check equality.</param>
    /// <param name="score2">The second score for which to check equality.</param>
    /// <returns>true if score2 and score1 represent the same score; otherwise, false.</returns>
    public static bool operator ==(Score score1, Score score2)
    {
        if (object.ReferenceEquals(score1, score2))
        {
            return true;
        }

        if ((object)score1 == null || (object)score2 == null)
        {
            return false;
        }

        return score1.CompareTo(score2) == 0;
    }

    /// <summary>
    ///  Determines whether two specified instances of Score are not equal.
    /// </summary>
    /// <param name="score1">The first score for which to check inequality.</param>
    /// <param name="score2">The second score for which to check inequality.</param>
    /// <returns>true if score2 and score1 represent a different score; otherwise, false.</returns>
    public static bool operator !=(Score score1, Score score2)
    {
        if (object.ReferenceEquals(score1, score2))
        {
            return false;
        }

        if ((object)score1 == null || (object)score2 == null)
        {
            return true;
        }

        return score1.CompareTo(score2) != 0;
    }

    /// <summary>
    /// Determines whether or not one specified Score is better than another specified Score.
    /// </summary>
    /// <param name="score1">The first score for which to check inequality.</param>
    /// <param name="score2">The second score for which to check inequality.</param>
    /// <returns>true if score1 represent a better score than score2; otherwise, false.</returns>
    public static bool operator >(Score score1, Score score2)
    {
        if (object.ReferenceEquals(score1, score2))
        {
            return false;
        }

        if ((object)score1 != null && (object)score2 == null)
        {
            return true;
        }

        if ((object)score1 == null && (object)score2 != null)
        {
            return false;
        }

        return score1.CompareTo(score2) > 0;
    }

    /// <summary>
    /// Determines whether or not one specified Score is worse than another specified Score.
    /// </summary>
    /// <param name="score1">The first score for which to check inequality.</param>
    /// <param name="score2">The second score for which to check inequality.</param>
    /// <returns>true if score1 represent a worse score than score2; otherwise, false.</returns>
    public static bool operator <(Score score1, Score score2)
    {
        if (object.ReferenceEquals(score1, score2))
        {
            return false;
        }

        if ((object)score1 != null && (object)score2 == null)
        {
            return false;
        }

        if ((object)score1 == null && (object)score2 != null)
        {
            return true;
        }

        return score1.CompareTo(score2) < 0;
    }

    /// <summary>
    /// Determines whether or not one specified Score is better than or equal to another specified Score.
    /// </summary>
    /// <param name="score1">The first score for which to check inequality.</param>
    /// <param name="score2">The second score for which to check inequality.</param>
    /// <returns>true if score1 represent a score that is better than or equal to score2; otherwise, false.</returns>
    public static bool operator >=(Score score1, Score score2)
    {
        if (object.ReferenceEquals(score1, score2))
        {
            return true;
        }

        if ((object)score1 != null && (object)score2 == null)
        {
            return true;
        }

        if ((object)score1 == null && (object)score2 != null)
        {
            return false;
        }

        return score1.CompareTo(score2) >= 0;
    }

    /// <summary>
    /// Determines whether or not one specified Score is worse than or equal to another specified Score.
    /// </summary>
    /// <param name="score1">The first score for which to check inequality.</param>
    /// <param name="score2">The second score for which to check inequality.</param>
    /// <returns>true if score1 represent a score that is worse than or equal to score2; otherwise, false.</returns>
    public static bool operator <=(Score score1, Score score2)
    {
        if (object.ReferenceEquals(score1, score2))
        {
            return true;
        }

        if ((object)score1 != null && (object)score2 == null)
        {
            return false;
        }

        if ((object)score1 == null && (object)score2 != null)
        {
            return true;
        }

        return score1.CompareTo(score2) <= 0;
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to the specified Score instance.
    /// </summary>
    /// <param name="other">A Score instance to compare to this instance.</param>
    /// <returns>true if the value parameter equals the value of this instance; otherwise, false.</returns>
    public bool Equals(Score other)
    {
        return this == other;
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to the specified object.
    /// </summary>
    /// <param name="obj">An object to compare to this instance.</param>
    /// <returns>true if the value parameter is a Score instance and equals the value of this instance; otherwise, false.</returns>
    public override bool Equals(object obj)
    {
        var o = obj as Score;

        if (o == null)
        {
            return false;
        }

        return this.Equals(o);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public abstract override int GetHashCode();

    /// <summary>
    /// Compares the value of this instance to a specified Score value
    /// and returns an integer that indicates whether this instance is better than,
    /// the same as, or worse than the specified Score value.
    /// </summary>
    /// <param name="other">A Score object to compare.</param>
    /// <returns>
    /// A signed number indicating the relative values of this instance and the value
    /// parameter.  Value Description: Less than zero: This instance is worse than
    /// value. Zero: This instance is the same as value. Greater than zero: This instance
    /// is better than value.
    /// </returns>
    public abstract int CompareTo(Score other);

    /// <summary>
    /// Adds this instance to the specified score.  Used in overloading the '+' operator.
    /// </summary>
    /// <param name="addend">The other score to add to this instance.</param>
    /// <returns>A new instance of Score representing the sum of this instance and the addend.</returns>
    public abstract Score Add(Score addend);
}

public sealed class HighestPointsScore : Score
{
    /// <summary>
    /// Initializes a new instance of the HighestPointsScore class with the specified number of points.
    /// </summary>
    /// <param name="points">The number of points that the new instance will represent.</param>
    public HighestPointsScore(double points)
    {
        this.Points = points;
    }

    /// <summary>
    /// Gets the number of points that this score represents.
    /// </summary>
    public double Points
    {
        get;
        private set;
    }

    /// <summary>
    /// Converts the points value of this instance to its equivalent string representation.
    /// </summary>
    /// <returns>The string representation of the value of this instance.</returns>
    public override string ToString()
    {
        return this.Points.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        return (int)this.Points;
    }

    /// <summary>
    /// Compares the value of this instance to a specified Score value
    /// and returns an integer that indicates whether this instance is better than,
    /// the same as, or worse than the specified Score value.
    /// </summary>
    /// <param name="other">A Score object to compare.</param>
    /// <returns>
    /// A signed number indicating the relative values of this instance and the value
    /// parameter.  Value Description: Less than zero: This instance is worse than
    /// value. Zero: This instance is the same as value. Greater than zero: This instance
    /// is better than value.
    /// </returns>
    public override int CompareTo(Score other)
    {
        var o = other as HighestPointsScore;

        if (o == null)
        {
            throw new InvalidOperationException();
        }

        return this.Points.CompareTo(o.Points);
    }

    /// <summary>
    /// Adds this instance to the specified score.  Used in overloading the '+' operator.
    /// </summary>
    /// <param name="addend">The other score to add to this instance.</param>
    /// <returns>A new instance of Score representing the sum of this instance and the addend.</returns>
    public override Score Add(Score addend)
    {
        if (addend == null)
        {
            return new HighestPointsScore(this.Points);
        }

        var a = addend as HighestPointsScore;

        if (a == null)
        {
            throw new InvalidOperationException();
        }

        return new HighestPointsScore(this.Points + a.Points);
    }
}

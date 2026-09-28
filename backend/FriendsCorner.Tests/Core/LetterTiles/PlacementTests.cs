namespace FriendsCorner.Tests;

public class PlacementTests
{
    private static Tile?[] EmptyBoard() => new Tile?[PremiumLayout.Squares];

    private static Tile[] Rack(string letters) =>
        letters.Select(letter => letter == Tile.Unassigned ? Tile.Blank : new Tile(letter)).ToArray();

    private static PlacedTile[] Play(params (int Square, char Letter)[] tiles) =>
        tiles.Select(tile => new PlacedTile(tile.Square, new Tile(tile.Letter))).ToArray();

    private static Tile?[] WithCat()
    {
        var board = EmptyBoard();
        board[111] = new Tile('C');
        board[112] = new Tile('A');
        board[113] = new Tile('T');
        return board;
    }

    [Fact]
    public void Cat_across_the_centre_is_a_legal_first_play()
    {
        Assert.Null(Placement.Check(EmptyBoard(), Rack("CATSEEN"), Play((111, 'C'), (112, 'A'), (113, 'T'))));
    }

    [Fact]
    public void The_first_play_must_cover_the_centre()
    {
        Assert.Equal(
            RefusalReason.FirstMustCoverCentre,
            Placement.Check(EmptyBoard(), Rack("CATSEEN"), Play((0, 'C'), (1, 'A'), (2, 'T'))));
    }

    [Fact]
    public void The_first_play_needs_two_tiles()
    {
        Assert.Equal(
            RefusalReason.FirstNeedsTwoTiles,
            Placement.Check(EmptyBoard(), Rack("CATSEEN"), Play((112, 'A'))));
    }

    [Fact]
    public void Tiles_must_share_a_row_or_a_column()
    {
        Assert.Equal(
            RefusalReason.NotInLine,
            Placement.Check(EmptyBoard(), Rack("CATSEEN"), Play((112, 'A'), (128, 'T'))));
    }

    [Fact]
    public void The_line_must_have_no_gap()
    {
        Assert.Equal(
            RefusalReason.Gap,
            Placement.Check(EmptyBoard(), Rack("CATSEEN"), Play((111, 'C'), (113, 'T'))));
    }

    [Fact]
    public void Tiles_already_on_the_board_close_a_gap()
    {
        Assert.Null(Placement.Check(WithCat(), Rack("SSEEN"), Play((110, 'S'), (114, 'S'))));
    }

    [Fact]
    public void A_later_play_must_touch_the_board()
    {
        Assert.Equal(
            RefusalReason.NotConnected,
            Placement.Check(WithCat(), Rack("ATSEEN"), Play((0, 'A'), (1, 'T'))));
    }

    [Fact]
    public void A_tile_the_player_does_not_hold_is_refused()
    {
        Assert.Equal(
            RefusalReason.NotInRack,
            Placement.Check(EmptyBoard(), Rack("CATSEEN"), Play((112, 'Q'), (113, 'I'))));
    }

    [Fact]
    public void Two_of_a_letter_need_two_on_the_rack()
    {
        Assert.Equal(
            RefusalReason.NotInRack,
            Placement.Check(EmptyBoard(), Rack("CATSEEN"), Play((112, 'T'), (113, 'T'))));
    }

    [Fact]
    public void A_blank_stands_for_any_letter_the_rack_lacks()
    {
        var play = new[] { new PlacedTile(112, Tile.Blank.As('Q')), new PlacedTile(113, new Tile('I')) };

        Assert.Null(Placement.Check(EmptyBoard(), Rack("?IATSEN"), play));
        Assert.Equal(RefusalReason.NotInRack, Placement.Check(EmptyBoard(), Rack("CIATSEN"), play));
    }

    [Theory]
    [InlineData(112)]
    [InlineData(225)]
    [InlineData(-1)]
    public void An_occupied_or_off_board_square_is_not_on_the_line(int square)
    {
        Assert.Equal(
            RefusalReason.NotInLine,
            Placement.Check(WithCat(), Rack("SEEN"), Play((square, 'S'), (114, 'E'))));
    }

    [Fact]
    public void An_empty_play_is_refused()
    {
        Assert.Equal(RefusalReason.NotInLine, Placement.Check(EmptyBoard(), Rack("CATSEEN"), []));
    }
}

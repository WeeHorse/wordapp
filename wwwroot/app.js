// KODEXEMPEL 1

$('#word-check').on('submit', testWord) // onsubmit for the testWord form

async function testWord(e) {
  e.preventDefault(); // not reload page on form submit
  const word = $('[name="word"]').val();
  console.log('word', word);
  const response = await fetch('/test-word/' + word); // get (read)
  console.log('response', response);
  const data = await response.json();
  console.log('data', data);
  $('#message').text(word + (data ? ' finns ' : ' finns inte ') + ' i databasen')
}

$('#new-word').on('submit', saveWord) // onsubmit for the saveWord form

async function saveWord(e) {
  e.preventDefault(); // not reload page on form submit
  const newWord = $('[name="new-word"]').val();
  console.log('newWord', newWord);
  const response = await fetch('/new-word/', { // post (save new)
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ word: newWord })
  });
  console.log('response', response);
  const data = await response.json();
  console.log('data', data);
  $('#message').text(newWord + ' lades till i databasen')
}

// KODEXEMPEL 2 - tictactoe

let players = [];

async function getPlayers() {
  const response = await fetch('/players/'); // get (read)
  console.log('response', response);
  players = await response.json();
  console.log('fetched players', players)
  if (players.length < 2) { // if we don't have two players we can't play
    $('#message2').text("We need TWO players, you only have " + players.length)
    return;
  }
  // let's use the last two players in the array
  players[0] = players[players.length - 2];
  players[1] = players[players.length - 1];
  players.length = 2;
  // assign tiles to the players
  players[0].tile = "X";
  players[1].tile = "O";
  togglePlayer(); // let the games begin
}
// load players initially
getPlayers();

$('#add-player').on('submit', addPlayer) // onsubmit for the addPlayer form

async function addPlayer(e) {
  e.preventDefault(); // not reload page on form submit
  const name = $('[name="name"]').val();
  console.log('name', name);
  const response = await fetch('/add-player/', { // post (save new)
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: name })
  });
  console.log('response', response);
  const data = await response.json();
  console.log('data', data);
  $('#message').text(player.name + ' lades till i databasen')
  // load players (so we get this last addition)
  getPlayers();
}

$('#tictactoe>input').on('click', playTile);
function playTile() {
  console.log($(this).index())
  $(this).val(players[0].tile)
  togglePlayer();
}

function togglePlayer() {
  players.push(players.shift());
  $('#message2').text("It's " + players[0].name + "s turn now, to lay an " + players[0].tile);
}


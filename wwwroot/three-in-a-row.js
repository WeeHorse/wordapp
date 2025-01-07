// KODEXEMPEL 2 - tictactoe

let players = [];
let thisPlayer = {};
let currentGame = {};


async function getPlayers() {
  const response = await fetch('/players/1'); // get (read) players from a game (id)
  console.log('response', response);
  players = await response.json();
  console.log('fetched players', players)
  if (players.length < 2) { // if we don't have two players we can't play
    $('#message2').text("We need TWO players, you only have " + players.length)
    return;
  }
  // let's use the last two players in the array (not proper)
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
  thisPlayer = data;
  $('#message').text(thisPlayer.name + ' lades till i databasen')
  // load players (so we get any update or new player)
  getPlayers();
}

$('#add-game').on('submit', addGame) // onsubmit for the addGame form

async function addGame(e) {
  e.preventDefault(); // not reload page on form submit
  const response = await fetch('/add-game/', { // post (save new)
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ 
      gameCode: $('[name="gamecode"]').val(),
      player_1: thisPlayer.id,
      player_2: 2   // hard coded value, change
    })
  });
  console.log('response', response);
  const data = await response.json();
  console.log('data', data);
  currentGame = data;
  $('#message2').text('Spelets anslutningskod är ' + currentGame.gamecode)
  // load players (so we get this last addition)
  getPlayers();
  activateFieldsForCurrentPlayer();
}

$('#tictactoe>input').on('click', playTile);
async function playTile() {
  let tileIndex = $(this).index();
  let game = 1 // hard coded
  $(this).val(players[0].tile)
  const response = await fetch('/play-tile/', { // post (save new move)
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ 
      player: players[0].id,
      tile: tileIndex,
      game: game 
    })
  });
  console.log('response', response);
  const data = await response.json();
  console.log('data', data);
  await checkWin(players[0], game);
}

async function checkWin(player, game) {
  const response = await fetch('/check-win/' + player.id + '/' + game);
  console.log('response', response);
  const win = await response.json();
  console.log('checked win', win);
  if(win){
    $('#message').text(player.name + ' vann med raden ' + win.join(' - '))
  }else {
    togglePlayer(0);
  }
}

function activateFieldsForCurrentPlayer(){
  if(players[0].id == thisPlayer.id){
    $('#tictactoe input').prop('disabled', false); // your turn
  }else{
    $('#tictactoe input').prop('disabled', true); // not your turn
  }
}

function togglePlayer() {
  players.push(players.shift());
  if(players[0].id == thisPlayer.id){
    $('#message').text("It's your turn now, " + players[0].name + " to lay an " + players[0].tile);
  }else{
    $('#message').text("It's " + players[0].name + "s turn to lay an " + players[0].tile);
  }
  activateFieldsForCurrentPlayer()
}


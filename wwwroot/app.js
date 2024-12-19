$('#word-check').on('submit', testWord)

async function testWord(e) {
  e.preventDefault(); // not reload page on form submit
  const word = $('[name="word"]').val();
  console.log('word', word);
  const response = await fetch('/test-word/' + word); // get (read)
  const data = await response.json();
  $('#message').text(word + (data ? ' finns ' : ' finns inte ') + ' i databasen')
}

$('#new-word').on('submit', saveWord)

async function saveWord(e) {
  e.preventDefault(); // not reload page on form submit
  const newWord = $('[name="new-word"]').val();
  console.log('newWord', newWord);
  const response = await fetch('/new-word/', { // post (save new)
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ word: newWord })
  });
  const data = await response.json();
  $('#message').text(newWord + ' lades till i databasen')
}
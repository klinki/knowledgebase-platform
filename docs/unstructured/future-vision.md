# Knowledgebase platform

I'm trying to build a knowledgebase platform that would allow me to have RAG.

Idea of features that I'd like to have:
- ability to send browser bookmarks from Chrome for analysis
- ability to send X tweets for analysis

And in second stage:
- ability to send text
- ability to send telegram messages for analysis - they could contain URL (to web or X tweet) or pure text

Idea is to: 

1) Categorize incoming items based on user pre-defined categories
2) Categorize based on LLM recommended categories (it could even create a new set of categories)
3) Add information quality ranking - something like 0-100% scale where 0% doesn't provide any useful information and 100% is high quality source of information
4) Vectorize incoming items so it is easy to run intelligent searches


Other interesting ideas:

- Multiple dimensions tagging
    - like `Source: Twitter`, `Source: Social Media`, `Topic: AI`, `Language: English`, `Type: Article`, `Type: Tweet`, `Type: Video`

- Have video analysis, automatically analyze most interesting parts
    - Maybe even segmenting minute by minute and ranking each segment by interest level

- Maybe generate something like FOAM or index etc.

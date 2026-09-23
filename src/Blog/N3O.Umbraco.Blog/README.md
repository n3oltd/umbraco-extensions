# N3O.Umbraco.Blog

Content types and querying for a blog: a blog page, a posts container, posts and categories.
`IBlogPostsFinder.FindPosts` takes criteria and returns posts as whichever strongly-typed content
model the caller asks for, so a site can extend the post type and still use the finder.

Posts are addressed by a URL that does not include the container node. A content finder resolves
those URLs and a URL provider generates them, so a post keeps one address whether it is linked from
the blog page or the posts container.

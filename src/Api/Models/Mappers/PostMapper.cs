namespace Api.Models;

using Mongo;
using Riok.Mapperly.Abstractions;

[Mapper]
public static partial class PostMapper
{
    public static partial PostCreated ToPostCreatedEvent(this Post user);
    public static partial PostViewModel ToPostViewModel(this Post user);

    public static partial IEnumerable<PostViewModel> ToPostViewModel(
        this IEnumerable<Post> q
    );
}
